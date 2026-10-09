# 8. Sessions and authentication

**Abstract.** A node has no user accounts. A client signs in by proving that it holds the private key
that belongs to an address. This chapter describes that proof, a challenge-response exchange, step by
step, and the session state it creates. It explains which checks are made and in which order, why an
address can be signed in on only one connection at a time, and how the node's own network bridge may
sign in on behalf of a peer. It then describes how signed-in calls use the session and what happens
when a connection closes.

## 8.1 Overview

A client's identity is its key pair, and its address is the SHA-256 hash of its public key
(Chapter 5). To sign in, a client asks the node for a random challenge, signs it with its private key
and sends back the signature together with its public key. If the signature is valid, the node links
the client's connection to the client's address. This link is the *session*. It exists only in memory
and ends when the connection closes.

Anyone can create a key pair, so anyone can sign in. Signing in does not give access to anything that
belongs to other users. It only proves which address the caller controls: the caller can then pull
the messages stored for that address and receive messages for it immediately.

## 8.2 Session state

`SessionManager` (`Hubs/Sessions/SessionManager.cs`) keeps the session state. Table 8.1 lists its
parts. All of them are plain collections that only the session thread changes (Chapter 3).

**Table 8.1:** Session state kept by `SessionManager`.

| Part | Content | Purpose |
|---|---|---|
| Pending challenges | connection ID → challenge (base64) | Challenges that have been issued and not yet used |
| Authenticated connections | set of connection IDs | Connections that have signed in. Kept, but not used for any access decision. |
| `ConnectionsMapper` | address → connection ID | The sessions: which connection is signed in as which address |

`ConnectionsMapper` is the part that matters. A lookup by address uses the dictionary directly. A
lookup by connection ID goes through all entries until it finds the connection.

## 8.3 The sign-in exchange

Listing 8.1 shows the exchange between a client `C` on connection `c` and a node. `pk_C` and `sk_C`
are the client's keys, and `N` is the size of `pk_C` in bytes.

**Listing 8.1:** The sign-in exchange.

```text
1. C -> node   GenerateToken()
2. node        n = 64 random bytes (cryptographic random source)
               remove any earlier challenge, sign-in and session of c
               pending[c] = base64(n)
3. node -> C   base64(n)

4. C           s = n ‖ RSA-SHA256-sign(sk_C, n)                      (Listing 5.3)
5. C -> node   Authenticate({ publicKey: pk_C, signature: base64(s) })

6. node        a = address(pk_C)
               e = pending[c]; remove pending[c]                     (single use)
               check: first |s| - N bytes of s = n (compared as base64 with e)
               check: RSA-SHA256-verify(pk_C, s)
               mapper[a] = c                                         (Section 8.4)
7. node -> C   true
```

Two properties follow from step 6. First, a challenge can be used only once. It is removed when
`Authenticate` is called, whether the attempt succeeds or not, so a failed attempt needs a new
challenge. Second, requesting a new challenge in step 2 signs the connection out: any earlier session
of the connection ends at that moment.

Table 8.2 lists the checks in the order they are made. The first four are made before the work is
queued on the session thread; the rest run on it.

**Table 8.2:** Checks made during `Authenticate`.

| Step | Check | Made by |
|---|---|---|
| 1 | `publicKey` is a PEM public key; `signature` is base64 | `ValidateModelFilter` (Chapter 7) |
| 2 | The address derived from `publicKey` is valid | `SessionManager.AuthenticateAsync` |
| 3 | If `X-Impersonate-Service` is set: the key is the node's own key (Section 8.5) | `RoutingHub` |
| 4 | If `X-Impersonate-Service` is set: its value is a valid address, and the key is the node's own key | `SessionManager.AuthenticateAsync` |
| 5 | A challenge is pending for this connection (it is removed now) | Session thread |
| 6 | The signed data equals the challenge | Session thread |
| 7 | The signature is valid for `publicKey` | Session thread |

If any check fails, `Authenticate` returns the same error, `Authentications Nonce signature could not
be verified.`, whatever the reason. A caller therefore cannot learn which check failed.

## 8.4 One connection per address

An address can be signed in on only one connection at a time. When a connection signs in as an
address that is already signed in elsewhere, `ConnectionsMapper` replaces the old connection with the
new one: the most recent sign-in wins. This happens, for example, when a client reconnects after a
network break, or when the same key is used on two devices.

The old connection stays open but loses its session. Its next call that needs sign-in fails with
`Authentication required`, and messages for the address are delivered only to the new connection.

## 8.5 Signing in on behalf of a peer

The network bridge of a node connects to its own hub once for each peer and must receive, on that
connection, the messages meant for the peer (Chapter 11). It therefore signs in with the node's own
key, but asks to be registered under the peer's address. It does this with the HTTP header
`X-Impersonate-Service: <peer address>` on the connection.

Two rules make sure that only the node itself can do this:

1. `RoutingHub.OnConnectedAsync` stores the header value when the connection opens. When
   `Authenticate` is called, `RoutingHub` passes the value on to `SessionManager` only if the public
   key in the request is the node's own public key. For any other key, the header is ignored and the
   caller is signed in under its own address.
2. `SessionManager.AuthenticateAsync` checks again that, if a header value is present, the key is the
   node's own key, and it also checks that the value is a valid address.

After a successful sign-in with the header, the session maps the peer's address, not the node's own
address, to this connection. Only a party that holds the node's private key can produce such a
session.

## 8.6 How signed-in calls use the session

For every hub method with the `[Authenticated]` attribute, `AuthenticatedFilter` asks
`SessionManager.TryGetAddressAsync` for the address mapped to the calling connection. If there is one,
it stores it in the hub property `ClientAddress`, and the hub method uses it: for example, `Pull2`
returns only the messages stored for `ClientAddress`. If there is none, the call fails with
`Authentication required`.

To deliver a message immediately, `OnionRoutingFilter` asks the opposite question with
`TryGetConnectionIdAsync`: which connection, if any, is signed in as the next address (Chapter 9).

## 8.7 When a connection closes

When a connection closes, `RoutingHub.OnDisconnectedAsync` asks `SessionManager.RemoveAsync` to end
its session. There are two cases.

**The connection had a session.** The session and any pending challenge are removed. The hub then
asks the network graph to remove the address from the node's list of neighbors (Chapter 10). The
node signs a new local vertex only if the address really was a neighbor, which is the case for peer
nodes but never for clients. The changed vertex is not broadcast at this point; peers receive it
with the next broadcast (Chapter 11).

**The connection had no session.** This is the case for connections that never signed in, and for
connections whose session was taken over by a newer sign-in (Section 8.4). Both are normal, so the
node only writes a Debug log entry, "disconnected without an active session".

In both cases the base class of the hub is then told that the connection has closed.

## 8.8 Properties and limits

- A session lasts as long as its connection. There is no other expiry.
- All sessions are lost when the node restarts; clients must sign in again (Chapter 4).
- Sign-in proves control of an address, nothing more. Any key pair can sign in.
- A challenge is valid for one attempt only, and a new challenge ends the connection's current
  session.
- An address has at most one session; the most recent sign-in wins.
- Only the node's own key can sign in on behalf of another address.

## 8.9 Summary

A client signs in by signing a random 64-byte challenge with its private key. The node checks that
the signed data is the challenge it issued and that the signature is valid, and then maps the
client's address to its connection. This mapping, kept in memory by `SessionManager`, is the session.
A challenge can be used once; an address has one session at a time, and the most recent sign-in wins.
The node's network bridge may sign in under a peer's address, but only with the node's own key.
Signed-in calls find the caller's address through the session. When a connection closes, its session
ends, and the node signs a new local vertex only if the address was one of its neighbors.
