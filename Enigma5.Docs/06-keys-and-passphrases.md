# 6. Keys, passphrases and the kernel keyring

**Abstract.** A node's identity is its RSA key pair. This chapter describes where the keys come
from, how they are created, and how the passphrase that protects the private key is obtained and
kept. The passphrase is stored in the Linux kernel keyring while the key is unlocked. The chapter
explains how the keyring is used, the difference between the `Persistent` and `Ephemeral` modes, and
why all work with the passphrase runs on one thread. It then describes `CertificateManager`, the
component that gives the rest of the code access to the keys, and the cost of using the key.

## 6.1 Overview

A node has one RSA key pair, stored in two PEM files. The public key identifies the node: the
node's address is the SHA-256 hash of it (Chapter 5). The private key is used for two things:
removing the node's layer of an onion, and signing (the node's graph entry and the sign-in
challenges of peers).

The private key file can be encrypted with a passphrase. The node then needs the passphrase every
time it opens the key. The passphrase is never written to disk by the node. While the key is
unlocked, it is kept in the Linux kernel keyring, and the native library reads it from there
whenever it opens the key file.

Three settings choose where the keys and the passphrase come from: `KeySource`,
`PassphraseSource` and `PassphrasePersistence`. [`README.md`](../README.md) describes the values of
each setting; this chapter describes what they do.

## 6.2 Key sources

The setting `KeySource` chooses the class that reads the key files. Table 6.1 lists the choices.

**Table 6.1:** Key sources.

| `KeySource` | Class | Behavior |
|---|---|---|
| `File` (default) | `FileKeyReader` | Reads the files named by `PublicKeyPath` and `PrivateKeyPath`. The files are read again on every call; nothing is cached. |
| `Azure` (planned for removal) | `AzureKeysReader` | Reads the public key from the Azure Key Vault secret named by `PublicKeyPath`. The private key, however, is still opened as a *file* at `PrivateKeyPath` whenever it is used, and new keys are written as files. This source is therefore only partly implemented. |

## 6.3 Creating keys

The class `KeysGenerator` creates keys by running the `openssl` program. Listing 6.1 shows the
commands.

**Listing 6.1:** Commands used to create a key pair.

```text
with a passphrase p:
    openssl genrsa -aes256 -out <private> -passout stdin 4096           (p on stdin)
    openssl rsa -in <private> -outform PEM -pubout -out <public> -passin stdin

without a passphrase:
    openssl genrsa -out <private> 4096
    openssl rsa -in <private> -outform PEM -pubout -out <public>
```

`CertificateManager.GenerateKeysAsync` decides what to create:

- if the private key file is missing or empty, both files are created;
- if only the public key file is missing or empty, it is exported from the private key;
- otherwise, nothing is done.

The passphrase is passed to `openssl` on its standard input, not on the command line, so other
processes cannot see it. The node checks the exit code of each command. If a command fails, key
creation reports failure, the node stays locked, and the log shows the command, its exit code and
the error message printed by `openssl`. The logged command contains only file paths, never the
passphrase. Chapter 4 describes when keys are created and why the default setup creates an
unencrypted key.

## 6.4 Passphrase sources

The setting `PassphraseSource` chooses where the passphrase comes from at startup. Table 6.2 lists
the choices. On the dashboard, the operator can always enter a passphrase, whatever this setting
is.

**Table 6.2:** Passphrase sources.

| `PassphraseSource` | Class | Behavior |
|---|---|---|
| `Dashboard` (default) | `DummyPassphraseProvider` | Returns an empty passphrase. The operator enters the passphrase on the dashboard. |
| `Keyboard` | `CommandLinePassphraseReader` | Asks for the passphrase on the console. The typed characters are not shown. At most 128 characters are accepted. |
| `Azure` (planned for removal) | `AzurePassphraseReader` | Reads the Azure Key Vault secret named by `PassphrasePath`. |

## 6.5 The kernel keyring

### 6.5.1 Why the keyring is used

The Linux kernel keyring is a place in kernel memory where processes can store small secrets. The
node uses it so that the passphrase does not have to stay in the memory of the .NET process. When
the native library opens an encrypted key file, OpenSSL asks for the passphrase through a
callback, and the library reads it from the keyring at that moment.

### 6.5.2 The keyring entry

The passphrase is stored as one keyring entry with these properties:

- type `user`, with the description `enigma5key: Key used for cryptographic operations.`;
- permissions that allow only the holder of the entry to view, read, change, search and set its
  attributes;
- a size of at most 256 bytes, which is also the passphrase limit shown on the dashboard.

The native library reads the passphrase as a text string that ends with a zero byte, and reads at
most 256 bytes. `SealProvider` therefore checks the passphrase before handing it over: it must be 1
to 256 bytes long in UTF-8 and must not contain a zero byte. A passphrase that breaks these rules is
rejected, and the node stays locked; without the check, such a passphrase would be cut short in the
keyring and would never match the key file. A valid passphrase is copied into a new array with a
zero byte at the end, and the copy is overwritten with zeros right after the native call.

### 6.5.3 Persistent and Ephemeral mode

The setting `PassphrasePersistence` chooses in which keyring the entry is stored. Table 6.3 compares
the two modes.

**Table 6.3:** `Persistent` and `Ephemeral` mode.

| | `Persistent` (default) | `Ephemeral` |
|---|---|---|
| Keyring | The persistent keyring of the Linux user that runs the node | The thread keyring of the node's key thread |
| After the node restarts | The passphrase is still there; the node unlocks itself | The passphrase is gone; the operator must unlock the node again |
| When it is removed | When the operator locks the key, or when the kernel expires the keyring: 3 days (the kernel default of 259 200 seconds) after the last use, since each use resets the timer | When the operator locks the key, or when the node process ends |
| Who can reach it | Any process of the same Linux user | Only the node's key thread |

The packaged service runs with `KeyringMode=private` (Appendix A). This gives the service its own
session keyring, but the persistent keyring still belongs to the Linux user and is shared by all its
processes.

### 6.5.4 The key thread

The native library keeps one handle to the passphrase entry for the whole process. Before every use
of the key, `CertificateManager` searches the keyring again, in the keyring chosen by the mode, to
refresh this handle. In `Ephemeral` mode, the entry lives in the keyring of one thread and can only
be found from that thread.

For these two reasons, `CertificateManager` does all its keyring work, and creates all decryptors and
signers, on one dedicated thread: the *key thread*, a `SimpleSingleThreadRunner` (Chapter 3). Only
the creation of a decryptor or signer runs there, because that is when the key file is opened and
the passphrase is read. Using the decryptor or signer afterwards does not need the keyring and runs
on the thread pool.

Listing 6.2 shows the steps for storing, using and removing the passphrase.

**Listing 6.2:** Keyring steps, all run on the key thread.

```text
Store passphrase p:
    search entry (refreshes the handle)
    remove the old entry, if any
    create a new entry with p                  (persistent or thread keyring)

Create a decryptor or signer:
    search entry (refreshes the handle)
    open the private key file; OpenSSL reads p from the entry if the file is encrypted

Remove passphrase:
    search entry (refreshes the handle)
    remove the entry
```

## 6.6 CertificateManager

`CertificateManager` (`Enigma5.Security/CertificateManager.cs`) is a singleton. It is the only way
the rest of the code reaches the keys. Table 6.4 lists its methods.

**Table 6.4:** Methods of `CertificateManager`.

| Method | What it does | Runs on the key thread |
|---|---|---|
| `SetupAsync(passphrase)` | Gets the passphrase (from the argument or, if it is empty, from the passphrase source), creates missing keys and stores the passphrase in the keyring. Called at startup and on unlock (Chapter 4). | The keyring part |
| `GenerateKeysAsync(passphrase)` | Creates missing key files (Section 6.3). | No |
| `CreateMasterPassphraseAsync(bytes)` | Stores the passphrase in the keyring (Listing 6.2). | Yes |
| `RemoveMasterPassphraseAsync()` | Removes the passphrase from the keyring. | Yes |
| `CreateUnsealerAsync()` | Returns a decryptor for onion layers. The node's public key is read first and passed to the decryptor for its size checks (Chapter 5). | Yes, except reading the public key |
| `CreateSignerAsync()` | Returns a signer. | Yes |
| `CanSignAsync()` | Tells whether the private key can be used right now, by signing one byte. Used by the network bridge (Chapter 11). | Yes |
| `GetPublicKeyAsync()`, `GetAddressAsync()` | Read the public key and compute the node's address. | No |
| `GetExportedContactDataAsync()` | Returns the data shown as a QR code on the dashboard (Section 6.8). | No |
| `GetPrivateKeyAsync()` | Reads the private key text. Not used. | No |

The caller disposes each decryptor and signer after use, which frees the native context.

## 6.7 Cost of using the key

A new decryptor is created for every onion, and a new signer for every signature. Each time, the key
file is read and, if it is encrypted, decrypted with the passphrase. Only this step runs on the key
thread; the decryptions themselves run in parallel on the thread pool (Chapter 3).

## 6.8 Contact data for clients

The dashboard shows a QR code that clients scan to use the node. It holds the result of
`GetExportedContactDataAsync`, written as canonical JSON (Chapter 5): the node's `Host` (setting
`Hostname`), `OnionService`, `Address` and `PublicKey`.

## 6.9 Tools for operators

On packaged installations, three commands help the operator manage the key files: `aenigma-keys`
creates a new encrypted key pair, `aenigma-lock-key` encrypts an existing private key, and
`aenigma-unlock-key` removes the encryption. They are described in Appendix A.

## 6.10 Summary

A node's identity is an RSA key pair stored in two PEM files. The node creates missing keys with
`openssl`, with a passphrase if one is available. The passphrase comes from the dashboard, the
console or, for now, Azure Key Vault, and is kept in the Linux kernel keyring while the key is
unlocked. In `Persistent` mode it survives restarts, for up to three days after its last use; in
`Ephemeral` mode it lasts only as long as the process. Because the native library keeps one
process-wide handle to the entry, and because an `Ephemeral` entry belongs to one thread,
`CertificateManager` does all keyring work and creates all decryptors and signers on its key thread.
Using a decryptor or signer then runs on the thread pool.
