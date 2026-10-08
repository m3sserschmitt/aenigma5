# 11. Federation: the network bridge

**Abstract.** Nodes exchange messages and graph updates through the *network bridge*, a component
that connects a node to the peers its operator has configured. This chapter describes how peers are
configured and how the bridge builds a pair of connections for each peer. It explains how the bridge
signs in on both sides and forwards calls between them. It then follows one run of the bridge step
by step:
connecting, signing in, announcing the node's neighbors, and moving and confirming stored messages.
The chapter ends with how the bridge handles failures and how it reaches peers over Tor.

## 11.1 Peers

A *peer* is a node that this node connects to. Peers are stored in the `Peers` table of the
database with two values. `Host` is the base URL of the peer's public endpoint, for example
`http://….onion` or `https://node.example.com`. `Address` is the peer's address. The operator adds
and removes peers on the dashboard (Chapter 16). `AddPeerHandler` accepts a peer only if the address
is valid and not stored yet, and the host is an absolute URL.

Only one side needs to configure the other. When node A lists node B as a peer, A's bridge connects
to B and signs in. A then has a session on B, so B accepts A as a neighbor (Chapter 10).

## 11.2 Connection vectors

For each peer, the bridge creates a `ConnectionVector` (`NetworkBridge/ConnectionVector.cs`): a pair
of SignalR client connections, shown in Table 11.1.

**Table 11.1:** The two connections of a connection vector for peer P.

| Connection | Goes to | Signs in as | Purpose |
|---|---|---|---|
| *Source* | This node's own hub, through the public endpoint (`Kestrel:EndPoints:Http`) | P's address, using the node's own key and the header `X-Impersonate-Service: <P's address>` (Chapter 8) | Receives everything this node sends to P |
| *Target* | P's hub, at P's `Host` | This node's own address | Receives everything P sends to this node |

The source connection holds a session for P's address on this node. So anything this node sends to
P, such as a message delivery or a graph broadcast, arrives on the source connection. In the same
way, the target connection holds a session for this node's address on P, so anything P sends to
this node arrives on the target connection.

Both connections use the hub settings of Chapter 7 on the client side. They reconnect automatically
after 2, 4, 8 and 16 seconds, send a keep-alive every 20 seconds, wait at most 90 seconds for the
server, and use stateful reconnect.

## 11.3 Forwarding

The bridge connects the two connections of a vector to each other for two hub methods:

**Listing 11.1:** Forwarding between the two connections of a vector for peer P.

```text
this node calls RouteMessage or Broadcast on the source connection
    -> the bridge calls the same method on P's hub through the target connection
       (P sees a call from this node's address)

P calls RouteMessage or Broadcast on the target connection
    -> the bridge calls the same method on this node's hub,
       through the source connection
       (this node sees a call from P's address)
```

So a message or graph update that this node sends to P is delivered to P's hub as if this node had
called P directly, and the other way around.

## 11.4 One run of the bridge

`Bridge.StartAsync` performs one complete run, on the bridge's runner thread (Chapter 3). Listing
11.2 shows its steps. Each step runs for all vectors, and every step runs even if an earlier one
failed for some vectors.

**Listing 11.2:** Steps of one run of the bridge.

```text
1. Load        read the peers from the database; create vectors for new peers,
               keep vectors that still exist, stop vectors of removed peers
2. Connect     open the target connection, then the source connection, of each vector
3. Sign in     for each vector:
                 a. read P's local vertex (GetLocalVertex) and check it
                    (Table 10.2); its address must be P's configured address
                 b. sign in to P's hub with the node's key
                    (GenerateToken, Authenticate)
                 c. sign in to this node's own hub as P
                    (same exchange, with the header)
4. Announce    open a short connection to this node's control endpoint,
               sign in with the node's key, and call TriggerBroadcast
               with the addresses of all signed-in peers
5. Synchronize for each vector, in both directions:
                 pull all stored messages page by page (Pull2) from one side
                 and route each of them to the other side (RouteMessage)
6. Confirm     for each direction, confirm the forwarded messages with Cleanup2,
               up to the last Id that was pulled
```

Step 4 adds the peers as neighbors of this node, signs a new local vertex and sends it to the
neighbors. It runs on the control endpoint because the default configuration blocks
`TriggerBroadcast` on the public endpoint (Chapter 14).

Step 5 moves messages that were stored while a peer was not connected. Messages that arrive while the
vector is connected are forwarded at once (Listing 11.1) and also stored. Step 5 of a later run finds
them again, and the receiving node recognizes them by their `uuid` (Chapter 9).

In step 6, a peer that runs an older version without `Cleanup2` answers that the method does not
exist. The bridge then calls `Cleanup` instead, so that federation with older nodes keeps working.

## 11.5 When the bridge runs

A run starts:

- right after the key setup succeeds, at startup or on unlock (Chapter 4);
- every five minutes, as a repeating background job registered at that moment (Chapter 17);
- when the operator adds or removes a peer, or presses *Retry* on the dashboard;
- after a vector has closed (Section 11.6).

Runs started in the first three ways also refresh the list of configured peers shown on the
dashboard; a retry after a closed vector does not.

### 11.5.1 Runs on a locked node

Every run begins with a check of the private key. The bridge asks `CertificateManager` to sign one
byte (`CanSignAsync`). This is a test of the key itself, so its answer cannot be out of date. The
node's own vertex would not do as a test: it stays signed in memory when the key is locked from
outside the process, for example by encrypting the key file with `aenigma-lock-key`.

If the key cannot sign, the run stops all vectors and ends. No vector is created, so none can fail
and be retried. When a vector is stopped, the peer loses the node's session and removes the node
from its neighbors (Chapter 10); the network map then no longer leads clients to a node that cannot
decrypt.

If the answer differs from what the node currently shows, the bridge also brings the node's state
into line. It logs a warning, has the local vertex generated again (unsigned if the key is gone,
signed if it is back), and sets the key state shown on the dashboard.

Locking on the dashboard starts a run for this purpose, so the connections close at once. After a
lock from outside, the same happens at the next scheduled run. The job stays registered while the
node is locked, so that the node connects again by itself once the key can be used.

This covers only the vectors the locked node created itself. A node that has connected *to* the
locked node, in one-sided peering, keeps its vector: the hub of the locked node still runs, and the
locked node cannot sign a vertex to announce the change. The connecting node
therefore goes on listing the locked node as a neighbor (Chapter 20).


## 11.6 Failures and retries

If a step fails for a vector, for example because the peer cannot be reached, signing in fails or a
message cannot be forwarded, the bridge stops that vector. When one of its connections closes, the
bridge removes the vector, waits `Network:DelayBetweenConnectionRetries` milliseconds (3000 by
default) and starts a new run, which creates the vector again.

Short network breaks are covered by the automatic and stateful reconnect of the SignalR client. A
break that the client can resume keeps the connection and its session. A reconnect that cannot be
resumed, for example because the peer restarted, gives the hub on the other side a new connection,
and a new connection is not signed in.

The vector therefore treats every reconnect that the client reports (its `Reconnected` event) like
a closed connection: it stops itself. The bridge then removes it and creates a new one after the
usual delay, and the new vector signs in on both sides again. Stopping the vector also ends the
peer's session on the node's own hub. The node removes the peer from its neighbors and signs its
vertex, but does not send it. About three seconds later, the new run adds the peer again and
broadcasts the vertex with the peer in it (Chapter 10).


### 11.6.1 A peer that changes its address

A peer gets a new address when its key is replaced. The peer must be restarted for this, so the
vector reconnects and is created again, as described above. The new vector asks the peer for its
vertex and finds an address that differs from the stored one. It refuses to sign in, logs
"Unexpected target address", and is stopped. The bridge keeps trying every few seconds and keeps
failing, until the operator corrects the stored peer (Chapter 16). A vector with the old address
is therefore never used against a peer with a new key.

The bridge keeps one vector for each host: vectors are compared by the host and port of their two
connections, not by the peer's address. If two stored peers have the same host, only one of them
gets a vector, and the code does not define which one. A host stands for one node, so two entries
for one host are always a mistake. Chapter 16 gives the order in which an operator must replace an
entry to avoid this.

### 11.6.2 Runs and removals share one queue

The bridge does its work on a single-thread runner (Chapter 3). A run and the removal of a closed
vector are both work items of that runner, done in the order in which they were asked for. Two
consequences follow, because a start over Tor can last up to two minutes (Section 11.7), and other
runs are asked for in that time by the five-minute job or by the dashboard.

- **A run that was asked for before a failure is done before the removal.** It finds the failed
  vector still in the set and starts it again.
- **A removal is asked for a certain vector, but vectors are compared by host.** A removal asked
  for an old vector could take a newer vector for the same peer out of the set.

Three rules keep these cases harmless.

**Table 11.3:** Rules for restarted and removed vectors.

| Rule | Reason |
|---|---|
| Every start of a target connection over Tor gets a new proxy handler. | SignalR disposes the handler when a connection fails or stops. With one handler for the life of the vector, a second start would fail at once with `ObjectDisposedException`, without trying the network. |
| A removal takes only the very object it was asked for. | A late removal cannot take the vector that has replaced a closed one. |
| A vector that is connected again when its removal is done is kept, and a vector that leaves the set is always stopped. | A vector restarted with success is not torn down, and no connection stays open without being in the set. |

The rules treat the effects. The cause is that a closed vector can be started again at all;
Chapter 20 lists the change that would remove it.

## 11.7 Peers on Tor

If a peer's `Host` is a `.onion` URL, the target connection goes through the SOCKS5 proxy given by
the setting `Socks5Proxy`. By default, this is the local Tor service at `127.0.0.1:9050`. The code
limits such connections to the long polling transport. The source connection always goes directly to the node's own
endpoint.

Connecting over Tor is slow and uneven. A request to an onion service that cannot be reached fails
either within seconds or only when the HTTP client gives up after 120 seconds, and the bridge can
do nothing else during that time (Section 11.6.2).

## 11.8 Summary

The network bridge connects a node to each configured peer with a connection vector. The vector has
a source connection to the node's own hub, signed in as the peer, and a target connection to the
peer's hub, signed in as the node. It forwards message deliveries and graph broadcasts between the two, so each
node sees the other as a directly connected caller. One run connects and signs in on both sides, and
adds the peers as neighbors. It then moves stored messages in both directions and confirms them,
with a fallback for peers that do not yet support `Cleanup2`. The bridge runs at unlock, every five minutes, after
peer changes and after a vector closes, and it reaches `.onion` peers through Tor.
