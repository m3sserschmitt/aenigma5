# 10. The network graph

**Abstract.** Every node keeps a map of the nodes it knows and of their connections, called the
network graph. Clients read it to build onions. This chapter describes how the map is made up, how a
node signs its own entry, and which checks an entry from another node must pass. It then explains
when a node accepts a new or changed entry and how it changes its own list of neighbors. Finally,
it shows how changes spread through the network and how old entries are removed. The chapter ends with the version value
clients use to notice changes.

## 10.1 Structure

The graph is a set of *vertices*, one per node, kept in memory by the singleton `NetworkGraph`
(`Enigma5.App/Data/NetworkGraph.cs`). It is not saved to disk; after a restart it holds only the
node's own vertex until other vertices arrive (Chapter 2). All operations on it run on its own
runner thread, and callers always get copies (Chapter 3).

**Table 10.1:** Parts of a vertex.

| Part | Content |
|---|---|
| `PublicKey` | The node's public key in PEM format. |
| `Neighborhood` | The part that is signed: the node's `Address`, its `Hostname` and `OnionService` (how to reach it, both optional), its `Neighbors` (the addresses of its neighbors) and `LastUpdate` (when it was signed). |
| `SignedData` | The canonical JSON of the neighborhood followed by the node's signature, in base64 (Chapter 5). |

Two vertices are treated as the same vertex if they have the same address. Two neighborhoods are
treated as equal if their address, hostname, onion service and set of neighbors are equal; their
`LastUpdate` may differ.

## 10.2 The node's own vertex

The node's own vertex is called the *local vertex*. `Vertex.Factory.CreateAsync` builds it. It
takes the node's public key and address, the settings `Hostname` and `OnionService`, the current
neighbors and the current time. It writes the neighborhood as canonical JSON and signs it with the
node's private key.

The local vertex is signed again when:

- the key setup succeeds at startup or on unlock (Chapter 4);
- the node's own `TriggerBroadcast` runs, which the network bridge does every five minutes
  (Chapter 11); this refreshes `LastUpdate` even when nothing else changed;
- a neighbor is added or removed (Section 10.5).

While the key is locked, signing fails. The local vertex then has no signature, and the node keeps
its current neighbor list until it is unlocked.

## 10.3 Checks for a received vertex

A vertex received from another node is accepted only if it passes all checks of
`NetworkGraphValidationPolicy`, listed in Table 10.2.

**Table 10.2:** Checks for a received vertex.

| Check | Rule |
|---|---|
| Not expired | `LastUpdate` is less than `VertexLifetime` (30 minutes by default) ago. |
| Public key | `PublicKey` is a PEM public key. |
| Address | `Address` is valid and equals the address computed from `PublicKey`. |
| No self-loop | The node does not list itself as its own neighbor. |
| Neighbor addresses | Every neighbor address is valid. |
| Signature | The signed data equals the canonical JSON of the neighborhood, and the signature is valid for `PublicKey`. |

Because each vertex is signed by its own node, the nodes that pass it on cannot change it without
the change being detected.

## 10.4 Accepting a vertex

The hub method `Broadcast` passes a received vertex to `NetworkGraph.UpdateAsync`, which does the
following:

1. It rejects the vertex if any check of Table 10.2 fails.
2. It ignores the vertex if it is the node's own vertex.
3. It may change the node's own neighbor list (Section 10.5).
4. If the graph has no vertex with this address, it adds the vertex.
5. If it already has one, it replaces it only if the new vertex is newer. If the neighborhood is
   unchanged, the new vertex must be more than 6 minutes newer.
6. It returns the vertices that changed, so that the hub can pass them on (Section 10.6).

A vertex that is rejected in step 1 changes nothing and is not passed on, but the caller is not
told: `Broadcast` reports success in this case too, as it does for a vertex that is not new.

The 6-minute rule in step 5 limits how often unchanged vertices are passed on. A node refreshes its
vertex every five minutes, so an unchanged vertex is accepted and passed on about every ten minutes.
That is still well within the 30-minute lifetime.

## 10.5 The node's own neighbors

A node's own neighbor list changes in three ways:

**Adding.** A neighbor is added in two cases: when a received vertex lists this node as its
neighbor, or when the node's own `TriggerBroadcast` gives new addresses (Chapter 11). In both cases,
an address is added only if it **currently has an active session on this node** (Chapter 8). A
vertex passed on by another node can therefore never make this node add its owner as a neighbor;
only a node that is connected right now can. This also allows *one-sided peering*. Suppose only node A
lists node B as a peer. A's bridge signs in to B, so A has a session on B, and B therefore accepts A
as a neighbor.

**Removing.** A neighbor is removed when its session ends (Chapter 8), or when a received vertex
from that neighbor no longer lists this node. No session is needed for removing.

**Signing.** After a change, the node signs a new local vertex. If signing fails, for example because
the key is locked, the change is not made and the current local vertex is kept.

As a result, a node's neighbor list follows its live connections. Chapter 20 lists one consequence:
any party that keeps a signed-in connection open can become a neighbor in this way, even if it is not
a real relay.

## 10.6 How changes spread

When `Broadcast` changed the graph, the hub sends each changed vertex to every neighbor that has a
session on the node, by calling `Broadcast` on that neighbor's connection. Neighbors that are peers
receive it through the network bridge (Chapter 11). Each node that receives a changed vertex does
the same, so a change spreads through the whole network. It stops at nodes that already have the
same or a newer version, because those return no changed vertices in step 6.

`TriggerBroadcast` sends only the node's own vertex. It reports success also when it added no
neighbor, because the vertex is signed and sent again in every case; it answers with the warning
`Broadcast will not be triggered ...` only when the node has no signed vertex to send, as on a
locked node. A node that has just connected therefore learns
the vertices of more distant nodes over time, as they refresh and pass on their vertices.

## 10.7 Removing old vertices

The graph removes old vertices with one cleanup routine. It removes every vertex, except the node's
own, that matches one of two rules:

- **Expired:** its `LastUpdate` is more than `VertexLifetime` (30 minutes by default) ago.
- **Unlisted:** no vertex in the graph lists it as a neighbor, and its `LastUpdate` is more than
  `UnlistedVertexGracePeriod` (6 minutes by default) ago.

The cleanup runs after a vertex is replaced, after the node removes one of its neighbors, and every
five minutes as a background job (`graph-cleanup`, Chapter 17). The job makes sure that expiry is
enforced on every node, also on one where nothing else changes the graph. Without it, an old vertex
on a quiet node would stay for as long as the process runs.

The grace period for unlisted vertices has two reasons. Vertices of distant nodes arrive one by
one, and a vertex may arrive before the vertex that lists it; removing it at once would make the
node lose part of the map it is just learning. The rule still serves its purpose, which is to remove
nodes that have left the network: their former neighbors stop listing them, and they are removed
once the grace period has passed. With the default values this takes at most about eleven minutes
(six minutes of grace and up to five until the next job run), instead of 30.

The cleanup does not run when a new vertex is added.

## 10.8 Graph version

`GET /Info` returns a `graphVersion`. It is the SHA-256 hash of the canonical JSON of all vertices,
sorted by address, with only their address, hostname, onion service and neighbors (no keys,
signatures or times). Clients can compare it with the last value they saw and read `GET /Vertices`
only when it changed. While the local vertex has no signature, `graphVersion` is `null`.

## 10.9 Inbound peers on the dashboard

After the graph changes, it tells the dashboard which nodes list this node as their neighbor. The
dashboard shows them as *inbound peers* (Chapter 16).

## 10.10 Summary

The network graph is an in-memory set of signed vertices, one per node. Each vertex is signed by its
own node and must pass checks for age, key, address, self-loops, neighbor addresses and signature
before it is accepted. A newer vertex replaces an older one, but an unchanged one only after six
minutes, which limits traffic. A node adds a neighbor only if that neighbor has an active session on
it, and removes it when the session ends. This keeps the neighbor list in line with live connections
and still allows one-sided peering. Changes spread from neighbor to neighbor until they reach nodes
that already have them. Old or unlisted vertices are removed after replacements and removals, and a
hash of the graph tells clients when it has changed.
