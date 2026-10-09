# 9. Message routing and storage

**Abstract.** This chapter follows a message through one node. It describes how the hub method
`RouteMessage` removes the node's layer, stores the rest as a *pending message* and, if possible,
delivers it at once. It explains how the tracking value `uuid` makes routing safe to repeat. It then
shows how a recipient collects its messages with `Pull2` and confirms them with `Cleanup2`, and how
stored messages are finally deleted. It ends with the delivery paths for recipients who are offline,
online, or behind a peer node.

## 9.1 Overview

A node never reads the content of a message. For each onion it receives, it does three things:

1. it removes its own layer, which reveals the next address and the rest of the onion (Chapter 5);
2. it stores the rest as a pending message for the next address;
3. if the next address has a session on the node, it also sends the rest to that connection at once.

The stored copy is kept until the recipient confirms it, or until it expires. The recipient is
either the next node on the path (through the network bridge, Chapter 11) or the final client.

## 9.2 Pending messages

Pending messages are stored in the `Messages` table of the database (Chapter 13). Table 9.1 lists
their fields.

**Table 9.1:** Fields of a pending message (`PendingMessage`).

| Field | Content |
|---|---|
| `Id` | Number assigned by the database, increasing with every new message. Used for paging and confirmation. |
| `Destination` | Address of the next stop: a node or a client. |
| `Content` | The rest of the onion after this node's layer was removed, in base64. |
| `Uuid` | Tracking value of the message (Section 9.4). |
| `Sent` | `false` until the recipient confirms the message, then `true`. |
| `DateCreated`, `Timestamp` | When the message was stored (date, and Unix seconds used by the cleanup job). |
| `DateSent`, `SentTimestamp` | When the message was confirmed. |

## 9.3 Routing one onion

Listing 9.1 shows what happens for one payload of a `RouteMessage` call. The steps before the hub
method are done by the hub filters (Chapter 7); a call with several payloads repeats them for each
payload.

**Listing 9.1:** Processing of one payload `p` by `RouteMessage`, sent by a signed-in connection.

```text
1. OnionParsingFilter   next ‖ rest = UnsealOnion(p)
                        (on failure: error "Could not parse onion.")
                        uuid = caller's uuid if the call has one payload, else none
2. OnionRoutingFilter   dest = connection with a session for next, if any
3. RouteMessage         stored = CreatePendingMessage(next, base64(rest), uuid)
4.                      if stored is new and dest exists, call on dest:
                            RouteMessage({ payloads: [base64(rest)], uuid })
5.                      success if stored has the expected uuid    (Section 9.4)
```

The message is stored in every case, also when it is delivered at once in step 4. The delivery in
step 4 is not waited for (Chapter 7); the stored copy makes sure that the message is not lost if
the delivery fails.

`CreatePendingMessageHandler` checks that `next` is a valid address and that the content is base64,
and then writes the message through the single database writer (Chapter 13). The writer also
makes the check of Section 9.4.3, in the same step in which it stores the message.

## 9.4 The tracking value uuid

Each pending message has a `uuid`, a GUID that tracks the message while it travels from relay to
relay and lets a node recognize a message it has already received.

### 9.4.1 How the uuid is set

- If a `RouteMessage` call has exactly one payload and gives a `uuid`, that value is used, in its
  lowercase form.
- Otherwise, the node creates a new random GUID.

When the node passes the message on, it sends the same `uuid` along. This happens both in the
immediate delivery of step 4 and when the network bridge later forwards stored messages
(Chapter 11). A message therefore keeps the `uuid` it got at the first node, at every later node.

### 9.4.2 Rules for callers

Table 9.2 lists the rules. The first two are checked when the request is validated, the third by
`OnionParsingFilter` (Chapter 7).

**Table 9.2:** Rules for the `uuid` of a `RouteMessage` call.

| Rule | Applies to | If broken |
|---|---|---|
| A `uuid` must be a GUID. | Every caller | `One or more properties not in correct format.` |
| A `uuid` may only be given with exactly one payload. | Every caller | `A uuid can only be provided with a single payload.` |
| A relay sends exactly one payload per call, with its `uuid`. | Callers whose address is a neighbor of the node | `Relays must route one message per request, with its uuid.` |

So only clients may send several payloads in one call, and they do so without a `uuid`; the node
then gives each message a new one. Relays always pass messages one at a time with their `uuid`,
both when they deliver a message at once and when they forward stored messages. This keeps every
relayed message traceable.

### 9.4.3 Recognizing a message that was already received

When a `uuid` is given with the call, the node first checks whether a message with that `uuid`
already exists. The check and the storing are one step, so calls that arrive at the same time
cannot both store the message. If so, it does not store it again and does not deliver it again, but `RouteMessage`
still reports success. This makes routing safe to repeat: a message that reaches a node twice, for
example once by immediate delivery and once by the bridge's synchronization, is stored once.

The check has two limits, both listed as future improvements in Chapter 20:

- It looks for the `uuid` among all stored messages, whatever their destination. If a caller gives
  a `uuid` that already belongs to another message on the node, its message is not stored, although
  the call reports success. Callers must therefore use new random values.
- It only works while the earlier copy is still stored. Confirmed messages are deleted by the next
  cleanup run by default (Section 9.6), so a copy that arrives after that is stored again. The final
  recipient then recognizes the duplicate by its `uuid`.

## 9.5 Collecting and confirming messages

A recipient collects the messages stored for its address and then confirms them. The node marks
confirmed messages as delivered; it does not delete them at once (Section 9.6).

**Listing 9.2:** Collecting and confirming messages with `Pull2` and `Cleanup2`.

```text
after = null
repeat:
    page = Pull2({ infId: after })        up to 20 messages with Id > after, by Id
    if page is empty: stop
    process the messages; skip any uuid already seen
    Cleanup2({ supId: last Id of page })  marks messages with Id <= supId as delivered
    after = last Id of page
```

Two rules protect messages that the recipient has not received:

- `Cleanup2` marks only messages up to the given `supId`. Messages stored after the page was read
  have higher `Id` values and stay pending.
- The node remembers, per connection, the highest `Id` that `Pull` or `Pull2` returned on that
  connection, and never confirms beyond it. A `supId` that is too high is reduced to that value.
  If nothing was pulled on the connection, nothing is confirmed.

The older methods are still available for older clients but are deprecated:

- `Pull` returns up to 128 messages at once. Messages beyond the first 128 stay pending.
- `Cleanup` takes no `supId`. It confirms all messages up to the highest `Id` pulled on the same
  connection.

A message that was delivered at once (step 4 of Listing 9.1) is also returned by `Pull2` until it is
confirmed. Clients therefore receive some messages twice and recognize them by their `uuid`.

## 9.6 Deleting messages

A background job runs every five minutes (Chapter 17) and deletes:

- pending messages older than `MessageRetentionPeriod` (14 days by default), whether they were
  ever collected or not;
- delivered messages confirmed longer ago than `SentMessageRetentionPeriod` (0 by default, so they
  are deleted by the next run).

There is no limit on the number or size of messages stored for one address, other than these
retention times and the size limit of one onion (Chapter 5).

## 9.7 Delivery paths

Table 9.3 shows how a message reaches its next stop in each situation.

**Table 9.3:** Delivery paths of a stored message.

| Next stop | How it gets the message |
|---|---|
| A client that is connected and signed in | At once, by a `RouteMessage` call from the node; also later by `Pull2` until it confirms the message |
| A client that is not connected | By `Pull2` when it next connects, within the retention period |
| A peer node that is connected through the network bridge | At once: the node calls `RouteMessage` on the bridge's connection, and the bridge forwards the call to the peer (Chapter 11) |
| A peer node that is not connected right now | By the bridge's next synchronization, which pulls the stored messages, forwards them and confirms them with `Cleanup2` |
| An address that never connects | Nobody; the message is deleted when it expires |

## 9.8 Summary

For each onion it receives, a node removes its layer, stores the rest for the next address and, if
that address is connected, delivers it at once as well. The `uuid` of a message is kept along the
path and makes repeated routing harmless, so a caller must use new values. Only clients may send
several onions in one call, without a `uuid`; relays send one message per call, with its `uuid`.
Recipients collect messages in pages with `Pull2` and confirm them with `Cleanup2`. The node never
confirms more than a connection has actually pulled, so messages that arrive in between are not
lost. A background job deletes old pending messages after 14 days and confirmed ones right away by
default.
