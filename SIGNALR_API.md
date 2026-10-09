## Aenigma Real-Time API Reference (SignalR)

**Connection Endpoint:** `http://localhost:8080/OnionRouting`

This document describes the real-time hub interface exposed by Enigma5.App for sign-in,
message delivery, and network graph updates.

---

### Table of Contents

- [Connecting](#connecting)
- [Access Control](#access-control)
- [Session](#session)
- [Messages](#messages)
- [Network Graph](#network-graph)
- [Server-to-Client Messages](#server-to-client-messages)
- [Data Models](#data-models)

---

### Connecting

| | |
|---|---|
| Reconnect support | Enabled — a short window of recent activity (~1 MiB) can be recovered after a brief disconnect |
| Keep-alive interval | 20 seconds |
| Timeout | 90 seconds of silence from either side |
| Handshake timeout | 15 seconds |
| Reconnect backoff | 2s → 4s → 8s → 16s |
| Maximum incoming message size | 331,776 bytes (324 KiB); larger messages are dropped without a reply |

---

### Access Control

The server can be reached on more than one network listener at once, and specific actions
can be disabled per listener via the `HubBlacklists` configuration section.

**Current configuration:**
```json
"HubBlacklists": [
  {
    "Endpoint": "http://127.0.0.1:8080",
    "Items": [
      {
        "Methods": [
          "TriggerBroadcast"
        ]
      }
    ]
  }
]
```

**How it's evaluated:**
- No `HubBlacklists` entries at all → every action is allowed on every listener.
- Each entry's `Endpoint` is matched against the listener a connection actually came in on.
An `Endpoint` of `0.0.0.0` matches any listener sharing that port, regardless of address.
- A connection on a listener with no matching entry is unrestricted.
- A connection on a listener with a matching entry is blocked only from the specific
actions listed under that entry's `Items[].Methods` (case-insensitive) — everything else on
that listener remains available.
- If the server can't determine which listener a connection came in on, the action is
refused as a safety fallback, independent of any entry actually matching.

**On rejection:** the response is the same `Internal error` message used for unrelated
failures — there's no distinct "blocked by a restriction" message.

**Current effect:** on `127.0.0.1:8080`, only `TriggerBroadcast` is blocked; every other
action remains callable there.

---

### Session

### `GenerateToken`

Issues a random one-time challenge tied to the connection, to be signed and returned via
`Authenticate`.

**Request Payload** — none

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns a base64-encoded challenge of 64 cryptographically random bytes |
| Failure | `Failed to generate authentication Nonce due to internal errors.` |

**Notes:**
- Requesting a new challenge clears any sign-in progress the connection already had.
- A challenge is single-use: every `Authenticate` call consumes it, whether it succeeds or
  fails. Request a new challenge before each sign-in attempt.

---

### `Authenticate`

Completes sign-in by verifying a signed challenge against a public key.

**Request Payload** — [`AuthenticationRequestDto`](#authenticationrequestdto)

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns `true` |
| Failure | `Authentications Nonce signature could not be verified.` |
| Failure | `One or more required properties not provided.` |
| Failure | `One or more properties not in correct format.` |

The first failure message is returned for every rejected sign-in, including when no challenge
is pending (none was requested, or it was already consumed by an earlier attempt).

**Notes:**
- On success, the connection remains signed in as that identity for as long as it stays open.
  The identity's address is the SHA-256 hash of the public key.
- If another connection later signs in with the same identity, messages are delivered to the
  most recent connection.
- The `X-Impersonate-Service` connection header is reserved for the node's own network bridge.
  It is honored only when the signing key is the node's own key, and is ignored otherwise.

---

### `GetLocalVertex`

Returns this server's own entry in the network graph.

**Request Payload** — none

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns a [`VertexDto`](#vertexdto) |
| Failure | `Internal error` |

---

### Messages

### `Pull` — Deprecated

> Use `Pull2` instead. Will be removed in a future version.

Retrieves up to 128 pending messages for the signed-in identity, ordered by `id`. Requires
sign-in. Messages beyond the first 128 stay pending and are returned by later calls.

**Request Payload** — none

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns an array of [`PendingMessageDto`](#pendingmessagedto) |
| Failure | `Internal error` |
| Failure | `Authentication required` |

---

### `Pull2`

Retrieves a page of up to 20 pending messages for the signed-in identity, ordered by `id`.
Requires sign-in.

To page through all pending messages, start with `infId` set to `null`, then pass the `id` of the
last message received until an empty page is returned. Pulling does not mark messages as
delivered; call [`Cleanup`](#cleanup) after pulling for that.

**Request Payload** — [`PullRequestDto`](#pullrequestdto)

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns an array of [`PendingMessageDto`](#pendingmessagedto) |
| Failure | `One or more properties have invalid values.` (negative `infId`) |
| Failure | `Internal error` |
| Failure | `Authentication required` |

---

### `Cleanup`

Marks the signed-in identity's pending messages as delivered, up to the highest message `id`
that `Pull` or `Pull2` has returned **on the same connection**. Messages that were not returned on
this connection, including messages that arrived after the last pull, stay pending. If nothing has
been pulled on this connection, nothing is marked. Requires sign-in.

**Request Payload** — none

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns `true` |
| Failure | `Internal error` |
| Failure | `Authentication required` |

---

### `RouteMessage`

Forwards one or more encrypted message payloads toward their destination. Requires sign-in.

**Request Payload** — [`RoutingRequestDto`](#routingrequestdto)

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns `true` |
| Failure | `Failed to route or store message.` |
| Failure | `Could not parse onion.` |
| Failure | `Invalid data provided for method invocation.` |
| Failure | `One or more required properties not provided.` |
| Failure | `Too many payloads for one request.` |
| Failure | `One or more payloads exceed the maximum onion size.` |
| Failure | `One or more properties not in correct format.` |
| Failure | `Authentication required` |

**Note:** a single request can batch up to 20 payloads; each is decrypted one layer and
routed independently — one failing doesn't stop the rest. A recipient who's currently
connected receives the message immediately in addition to it being saved; otherwise it's
saved only, for later retrieval via `Pull2`.

---

### `Broadcast`

Accepts and forwards an adjacency update from a peer. Requires sign-in.

**Request Payload** — [`VertexBroadcastRequestDto`](#vertexbroadcastrequestdto)

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns `true` |
| Failure | `Failed to handle vertex broadcast; it will not be forwarded to other nodes.` |
| Failure | `Vertex broadcast could not be received by some of the neighbors.` |
| Failure | `One or more required properties not provided.` |
| Failure | `One or more properties not in correct format.` |
| Failure | `One ore more properties format could not be verified due to insufficient/malformed information.` |
| Failure | `Authentication required` |

---

### `TriggerBroadcast`

Adds the given addresses to this node's own neighbor list, re-signs the node's graph entry and
broadcasts it to its neighbors. Requires sign-in.

This method is meant for the node itself: its network bridge calls it through the control
endpoint (`HttpControl`, `127.0.0.1:8081` by default) after connecting to its peers. It is
blocked on the public endpoint (`127.0.0.1:8080`) by the default `HubBlacklists`
configuration, and clients should not call it.

**Request Payload** — [`TriggerBroadcastRequestDto`](#triggerbroadcastrequestdto)

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns `true` |
| Failure | `Broadcast will not be triggered because validation failures or no changes required to local neighborhood.` *(soft warning, not a hard error)* |
| Failure | `Failed to trigger broadcast or some neighbors were not able to receive.` |
| Failure | `One or more properties not in correct format.` |
| Failure | `Authentication required` |

---

### Server-to-Client Messages

The server also invokes methods on connected clients. A client registers handlers for them on
its hub connection (for example `connection.On<RoutingRequestDto>("RouteMessage", ...)`).

### `RouteMessage` (server → client)

Pushed to the connection signed in as a message's next-hop address, as soon as the message is
routed, if that address is currently connected.

**Payload** — [`RoutingRequestDto`](#routingrequestdto) with a single item in `payloads`: the
message content after this node removed its onion layer, base64-encoded. `uuid` is the
message's tracking reference.

**Note:** the message is also stored and remains available through [`Pull2`](#pull2) until the
recipient confirms it with [`Cleanup`](#cleanup). Use `uuid` to detect duplicates.

### `Broadcast` (server → client)

Pushed to the connections of this node's graph neighbors when the node's view of the network
changes.

**Payload** — [`VertexBroadcastRequestDto`](#vertexbroadcastrequestdto)

**Note:** only other nodes are graph neighbors, so regular clients never receive this message.

---

### Data Models

### `AuthenticationRequestDto`

| Property | Type | Description |
|---|---|---|
| `publicKey` | string | Caller's public key, in PEM format |
| `signature` | string | Base64 encoding of the decoded challenge bytes from `GenerateToken`, followed by their RSA SHA-256 signature made with the caller's private key |

---

### `PullRequestDto`

| Property | Type | Description |
|---|---|---|
| `infId` | integer or `null` | `id` of the last message already received; only messages with a greater `id` are returned. Omit or `null` for the first page; must not be negative |

---

### `VertexBroadcastRequestDto`

| Property | Type | Description |
|---|---|---|
| `publicKey` | string | Public key of the broadcasting peer, in PEM format |
| `signedData` | string | Signed, base64-encoded adjacency data; decodes into a `NeighborhoodDto`-shaped object once verified against `publicKey` |

---

### `TriggerBroadcastRequestDto`

| Property | Type | Description |
|---|---|---|
| `newAddresses` | string[] | Addresses of newly connected peers |

---

### `RoutingRequestDto`

| Property | Type | Description |
|---|---|---|
| `payloads` | string[] | One or more base64-encoded, onion-encrypted message layers (max 20, each at most 16,384 characters) |
| `uuid` | string or `null` | Caller-supplied tracking reference; only honored when `payloads` contains exactly one item |

---

### `VertexDto`

| Property | Type | Description |
|---|---|---|
| `publicKey` | string | Node's public key, in PEM format |
| `signedData` | string | Neighborhood serialized data, signed and base64-encoded |
| `neighborhood` | [`NeighborhoodDto`](#neighborhooddto) or `null` | Node neighborhood info |

---

### `NeighborhoodDto`

| Property | Type | Description |
|---|---|---|
| `address` | string | The node's own address |
| `hostname` | string | Base address for reaching this node directly, if configured |
| `onionService` | string | Onion-service address for reaching this node over Tor, if configured |
| `neighbors` | string[] | Addresses of this node's current connections |
| `lastUpdate` | datetime | When this entry was last refreshed |

---

### `PendingMessageDto`

| Property | Type | Description |
|---|---|---|
| `id` | integer | Numeric identifier for this message — this is the value to pass as `infId` when paging via `Pull2` |
| `uuid` | string or `null` | Tracking reference for the message, either caller-supplied (single-message sends) or server-generated (batched sends) |
| `destination` | string or `null` | Address the message is addressed to |
| `content` | string or `null` | The message's encrypted contents |
| `dateReceived` | datetime | When the server received/stored this message |
| `sent` | boolean | Whether the message has already been pushed live to a connected recipient, as opposed to only sitting in storage waiting to be pulled |

---
