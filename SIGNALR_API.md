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
| Success | Returns a 64-byte encoded challenge value |
| Failure | `Failed to generate authentication Nonce due to internal errors.` |

**Note:** requesting a new challenge clears any sign-in progress the connection already had.

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

**Note:** on success, the connection remains signed in as that identity for as long as it
stays open.

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

Retrieves up to 1024 pending messages for the signed-in identity. Requires sign-in.

**Request Payload** — none

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns an array of [`PendingMessageDto`](#pendingmessagedto) |
| Failure | `Internal error` |
| Failure | `Authentication required` |

---

### `Pull2`

Retrieves a page (20) of pending messages for the signed-in identity. Requires sign-in.

**Request Payload** — [`PullRequestDto`](#pullrequestdto)

**Responses**

| Outcome | Description |
|---|---|
| Success | Returns an array of [`PendingMessageDto`](#pendingmessagedto) |
| Failure | `Internal error` |
| Failure | `Authentication required` |

---

### `Cleanup`

Marks all pending messages for the signed-in identity as delivered. Requires sign-in.

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
| Failure | `One or more properties not in correct format.` |
| Failure | `Authentication required` |

**Note:** a single request can batch up to 20 payloads; each is decrypted one layer and
routed independently — one failing doesn't stop the rest. A recipient who's currently
connected receives the message immediately in addition to it being saved; otherwise it's
saved only, for later retrieval via `Pull2`.

---

### Network Graph

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

Announces this connection's own newly added peers to the network. Requires sign-in.
Currently blocked on `127.0.0.1:8080`.

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

### Data Models

### `AuthenticationRequestDto`

| Property | Type | Description |
|---|---|---|
| `publicKey` | string | Caller's public key, in PEM format |
| `signature` | string | The challenge from `GenerateToken`, signed with the caller's private key, base64-encoded |

---

### `PullRequestDto`

| Property | Type | Description |
|---|---|---|
| `infId` | integer or `null` | Marker to continue paging from; omit for the first page |

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
| `payloads` | string[] | One or more base64-encoded, onion-encrypted message layers (max 20) |
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
