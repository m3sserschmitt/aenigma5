# 5. Cryptography and onions

**Abstract.** This chapter describes how a node encrypts, decrypts, signs and checks data. All of
these operations are done by the native library `libaenigma.so`, which the node calls through the
project `Enigma5.Crypto`. The chapter explains how the library is loaded and used, and gives the
exact byte formats of addresses, encrypted envelopes, signed data and onion layers. It then shows
how clients build onions and how a node removes one layer, including the checks the node makes before
it hands data to the native library. The chapter ends with how to rebuild the library.

## 5.1 Overview

Table 5.1 lists the cryptographic operations a node uses and what it uses them for.

**Table 5.1:** Cryptographic operations and where they are used.

| Operation | Algorithm | Done by | Used for |
|---|---|---|---|
| Envelope encryption | Random AES-256 key, wrapped with RSA; data encrypted with AES-256-GCM | `libaenigma.so` | Onion layers (Sections 5.4.2 and 5.4.4) |
| Signing and checking | RSA with SHA-256 | `libaenigma.so` | Sign-in (Chapter 8), network graph entries (Chapter 10), shared data (Chapter 12) |
| Hashing | SHA-256 | .NET | Addresses (Section 5.4.1), network graph version (Chapter 10) |
| Random numbers | Operating system random source | .NET and OpenSSL | Sign-in challenges (.NET); AES keys and GCM nonces (OpenSSL) |
| Key pair creation | RSA, 4096 bits | `openssl` program | The node's own key pair (Chapter 6) |

`libaenigma.so` is built on OpenSSL. It does not set an RSA padding mode itself, so OpenSSL's
defaults apply: PKCS #1 v1.5 padding, both for wrapping the AES key and for signatures.

## 5.2 The native library

### 5.2.1 Loading

The prebuilt library is stored in `Enigma5.Crypto/runtimes/<platform>/native/libaenigma.so`, where
`<platform>` is `linux-amd64` or `linux-arm64`. The build copies this folder next to the program.

The class `Native` (`Enigma5.Crypto/Native.cs`) declares the library's functions with
`[LibraryImport]`. When the class is first used, it registers a resolver that looks for the library
in `runtimes/<platform>/native/` next to the program file. The platform is found by
`RuntimeHelpers.GetRuntimeIdentifier()`. Only Linux on `amd64` and `arm64` is supported. If the
library is not found there, the resolver tries to load `libaenigma.so` by name, from the system's
normal library paths.

### 5.2.2 Functions used

Table 5.2 groups the native functions the node calls.

**Table 5.2:** Native functions used by the node.

| Group | Purpose | Functions |
|---|---|---|
| Contexts | Create and free an object that holds a key and one kind of operation | `CreateAsymmetricEncryptionContext`<br>`CreateAsymmetricDecryptionContext`<br>`CreateAsymmetricDecryptionContextFromFile`<br>`CreateSignatureContext`<br>`CreateSignatureContextFromFile`<br>`CreateVerificationContext`<br>`FreeContext` |
| Operations | Encrypt, decrypt or sign; check a signature | `Run`<br>`RunVerification` |
| Onions | Remove one layer; read a layer's length prefix | `UnsealOnion`<br>`DecodeOnionSize` |
| Sizes | Sizes used to check input before native calls | `GetAddressSize`<br>`GetPKeySize`<br>`GetEnvelopeSize`<br>`GetSignedDataSize`<br>`GetKernelKeyMaxSize` |
| Keyring | Store, find and remove the passphrase in the kernel keyring (Chapter 6) | `SetMasterPassphraseName`<br>`CreateMasterPassphrase`<br>`CreatePersistentMasterPassphrase`<br>`SearchMasterPassphrase`<br>`SearchPersistentMasterPassphrase`<br>`RemoveMasterPassphrase` |

### 5.2.3 Contexts and memory

Each native operation works on a *context*: a native object that holds one key and is set up for
one kind of operation. `CryptoContext` wraps the pointer to such an object. It implements
`IDisposable` and frees the native object when it is disposed, or at the latest when the garbage
collector finalizes it.

Before a context is created, the key text is checked with a regular expression for the PEM format
(`IsValidPublicKey`, `IsValidPrivateKey`). If the check fails, no native call is made and the
context holds a null pointer. Every operation on such a context returns `null` or `false`.

The result of `Run` and `UnsealOnion` is a buffer that belongs to the context. The wrapper copies it
into a managed array right away; the buffer is freed together with the context. The node therefore
never frees a native buffer itself.

## 5.3 SealProvider

`SealProvider` (`Enigma5.Crypto/SealProvider.cs`) is the class the rest of the code uses. It
implements four interfaces, one per kind of operation, and its nested class `Factory` creates
instances for a given key.

**Table 5.3:** Interfaces of `SealProvider` and their use by the node.

| Interface | Method | Created by | Used by the node for |
|---|---|---|---|
| `IEnvelopeVerifier` | `Verify` | `Factory.CreateVerifier(publicKey)` | Checking sign-in challenges, graph entries and shared data |
| `IEnvelopeUnsealer` | `Unseal`, `UnsealOnion` | `Factory.CreateUnsealerFromFile(path, publicKey)` | Removing a layer of an onion |
| `IEnvelopeSigner` | `Sign` | `Factory.CreateSignerFromFile(path)` | Signing its own graph entry and sign-in challenges of peers |
| `IEnvelopeSealer` | `Seal` | `Factory.CreateSealer(publicKey)` | Not used by the node; used by tests |

`SealProvider` has no method that builds an onion. A node only removes layers; onions are built by
clients, with the native library (Section 5.5).

## 5.4 Data formats

The listings in this section use these symbols: `‖` joins two byte strings, `|x|` is the length of
`x` in bytes, and `N` is the size of an RSA key in bytes (512 for a 4096-bit key). All lengths are
in bytes.

### 5.4.1 Addresses

An address is derived from a public key in PEM format, as shown in Listing 5.1.

**Listing 5.1:** How an address is computed.

```text
der     = base64-decode(text between PEM header and footer)    (SubjectPublicKeyInfo)
address = lowercase-hex(SHA-256(der))                             (64 characters)
```

The code is in `CertificateHelper.GetHexAddressFromPublicKey`. An address is valid if it matches
`^[a-f0-9]{64}$`. Inside an onion, the next address is stored as its 32 raw bytes, not as text.

### 5.4.2 Envelopes

An *envelope* encrypts data for the owner of a public key. Listing 5.2 shows its layout.

**Listing 5.2:** Layout of an envelope for public key `pk` and plaintext `p`.

```text
k    = random AES-256 key (32 bytes)
iv   = random nonce (12 bytes)

E    = EK ‖ iv ‖ C ‖ T
EK   = RSA-encrypt(pk, k)                    |EK| = N
C    = AES-256-GCM-encrypt(k, iv, p)         |C|  = |p|
T    = GCM authentication tag                |T|  = 16

|E|  = N + 12 + |p| + 16                     overhead = N + 28
```

Decryption reverses these steps. Because GCM checks the tag, any change to an envelope makes
decryption fail.

### 5.4.3 Signed data

Signed data is the data followed by its signature, as shown in Listing 5.3.

**Listing 5.3:** Layout of signed data for private key `sk` and data `d`.

```text
S    = d ‖ sig
sig  = RSA-SHA256-sign(sk, d)                |sig| = N
d    = the first |S| - N bytes of S          (valid only if |S| > N)
```

To check `S`, the verifier needs only `S` and the public key; it takes the last `N` bytes as the
signature. `ByteArrayExtensions.GetDataFromSignature` extracts `d` in the same way.

### 5.4.4 Onion layers

An onion layer is an envelope with a length prefix. Its plaintext starts with the address of the
next stop. Listing 5.4 shows the layout.

**Listing 5.4:** Layout of one onion layer for node key `pk`, next address `a` and inner data `m`.

```text
O    = L ‖ E
L    = |E| as a 2-byte big-endian number     (so |E| <= 65 535)
E    = envelope(pk, a_raw ‖ m)               (Listing 5.2)
a_raw= the 32 raw bytes of address a
m    = the next onion layer (with its own prefix), or the final payload

overhead per layer = 2 + N + 12 + 16 + 32 = N + 62      (574 bytes for a 4096-bit key)
```

Each layer adds a fixed number of bytes. An onion therefore gets shorter by the same amount at
every node, and a node can tell from its size roughly how many layers are left. Chapter 19
discusses this.

The 2-byte prefix limits a layer to 65 535 bytes. The node accepts much less: an onion sent with
`RouteMessage` may be at most 16 384 base64 characters (`Constants.MaxOnionSize`), which is
12 288 bytes. This limit is sized for the largest message the Android client sends:

- a path of 6 relay nodes plus a final layer for the recipient, which the client removes itself
  with `UnsealOnion`: 7 layers of 574 bytes with 4096-bit keys;
- inside it, a signed message object of at most about 5 600 bytes: 256 characters of text in the
  worst JSON encoding, a 64-character sender name, URLs with 253-character domain names, and the
  sender's 4096-bit public key and signature.

Such an onion is at most 9 664 bytes, or 12 888 base64 characters, so the limit leaves about 27%
of room. Chapter 7 describes the check and the matching size limit of the hub.

## 5.5 Building an onion

A node never builds an onion, and `Enigma5.Crypto` does not import the function for it. Clients
build onions with the function `SealOnion` of the native library. It is described here because
its result is what a node receives. In this repository only the tool that creates the stored
onions of the tests calls it (`Enigma5.Tests.DataGenerator`, Chapter 21).

`SealOnion` takes the payload, a list of public keys and a list of addresses of the same length,
and builds the onion from the inside out. In each step it wraps the result of the previous step.
The first entries of the lists therefore make the innermost layer.

**Listing 5.5:** How the arguments of the native function `SealOnion` relate to Listing 1.1.

```text
path (n_1, ..., n_k), recipient r, payload m

keys      = [ pk(n_k),  pk(n_(k-1)),  ...,  pk(n_1) ]
addresses = [ a(r),     a(n_k),       ...,  a(n_2)  ]

result    = base64(O_1)
```

The caller must make sure that both lists have the same length, that every key is a PEM public key
and that every address is valid. The native function trusts its input.

## 5.6 Removing a layer

`SealProvider.UnsealOnion` removes one layer. The native library trusts the lengths it is given, so
the wrapper checks the input first. The needed sizes come from the native size functions, which
are called once when the `SealProvider` is created; only the 2-byte length of the prefix is a
constant in the C# code (`Constants.OnionLengthBytes`).

**Listing 5.6:** Steps of `UnsealOnion` for a base64 onion `s`, with node key size `N`.

```text
1. O = base64-decode(s)
2. reject if |O| < 2
3. L = first 2 bytes of O, as a big-endian number
4. reject if L != |O| - 2
5. reject if L - (N + 28) < 32          (too short to hold the next address)
6. P = native UnsealOnion(O)            (decrypts; fails if the GCM tag is wrong)
7. reject if |P| < 32
8. next    = lowercase-hex(first 32 bytes of P)
   content = the rest of P
```

The same idea protects the other operations. Table 5.4 lists all checks made before native calls.

**Table 5.4:** Input checks made before native calls.

| Method | Check | Size used |
|---|---|---|
| `UnsealOnion` | Steps 2 to 5 of Listing 5.6 | `GetEnvelopeSize(0, pk) = N + 28`, `GetAddressSize() = 32` |
| `Unseal` | Input longer than `N + 28` | `GetEnvelopeSize(0, pk)` |
| `Verify` | Input longer than `N` (at least one byte of data plus a signature) | `GetSignedDataSize(0, pk) = N` |

These sizes come from the public key given to the factory. For decryptors, the node passes its own
public key, because a decryptor is created from a private key file. If the key is missing or
invalid, the sizes are `-1` and every check fails, so no data reaches the native library.

## 5.7 Canonical JSON

Some signed data is JSON: the neighborhood of a network graph entry (Chapter 10). To sign and check
the same bytes on every node, the JSON must always be written the same way.
`ObjectExtensions.CanonicallySerialize` does this in two steps. It first serializes the object with
`System.Text.Json`, leaving out `null` values and keeping the .NET property names. It then rewrites
the text in the canonical form of RFC 8785 (fixed key order, fixed number format, no extra spaces).
The result is signed as ASCII bytes.

## 5.8 Rebuilding the native library

The source of the library is the git submodule `Libaenigma7`. The script
`Enigma5.Scripts/build-libs.sh` builds it for the current machine and for `arm64`, and copies the
results into `Enigma5.Crypto/runtimes/`. The required packages are listed in
[`README.md`](../README.md). Rebuilding is only needed after changes to the library; the prebuilt
files in the repository are used otherwise.

## 5.9 Summary

All encryption, decryption, signing and signature checking is done by `libaenigma.so`, which is
loaded from the `runtimes` folder next to the program. `SealProvider` wraps it with four interfaces
and checks every input size before a native call, because the library trusts the sizes it is given.
An address is the SHA-256 hash of a public key. An envelope is an RSA-wrapped AES-256-GCM
encryption with `N + 28` bytes of overhead. Signed data is the data followed by an `N`-byte
signature. An onion layer is an envelope with a 2-byte length prefix, whose plaintext starts with
the 32-byte next address. Removing a layer is the most expensive step of routing, and it runs in
parallel for different onions.
