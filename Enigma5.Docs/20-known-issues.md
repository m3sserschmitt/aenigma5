# 20. Known issues and technical debt

**Abstract.** This chapter collects the open problems, limitations and planned improvements that
the other chapters mention. Each entry states what the matter is, what it causes, and what could be
done about it, and points to the chapter with the details. The entries are grouped by kind: defects
in the code, limits of the protocol, limits in operation, and code that is no longer needed. The
chapter is a working list for contributors. Limits that belong to the design itself, such as the
missing protection against traffic analysis, are described in Chapter 19 and are not repeated here.

## 20.1 How to read this chapter

Every entry has an identifier, so that it can be named in discussions and commits. The letter
shows the group: `D` for defects, `P` for protocol limits, `O` for operation, `T` for technical
debt and `C` for work that must be done in the client. The entries describe the code at the time
of writing. An entry should be removed when the matter is settled, and its number is not used
again; this is why the numbers have gaps.

## 20.2 Defects

**Table 20.1:** Defects in the code.

| ID | Matter | Effect | Possible change | Chapter |
|---|---|---|---|---|
| D3 | A file and its record are stored and deleted in two steps. A failed write is undone, but two cases remain: the node stops between the steps of an upload, or a file cannot be deleted after its record was removed. | In the first case a record, perhaps with part of a file, stays until the cleanup job removes it. In the second case a file stays on disk without a record. | Let the cleanup job also delete files in the upload directory that have no record. | 13 |
| D8 | Only the first blacklist that matches an endpoint is used. | Rules in a second blacklist for the same endpoint have no effect, without a message. | Apply the items of all matching blacklists. | 14 |
| D11 | A node that locks its key closes only the connections it opened itself. | A node that connected to it keeps its vector and goes on listing the locked node as a neighbor and showing it as *Connected*, although the locked node cannot route. | Let a locked node end the sessions of its neighbors, so that they notice at once. | 11, 16 |
| D12 | A closed connection vector can be started again by a run that was asked for before its removal. | Since 5.1.0 the effects are contained by three rules (Section 11.6.2), but runs and retries still come in pairs after such a case. | Let a run replace a vector whose connection has closed instead of starting it again. | 11 |
| D13 | A request to a peer's onion service that cannot be reached may end only after 120 seconds, and the bridge does nothing else in that time. | After a break, a peer on Tor can stay disconnected for several minutes. | A shorter limit for the first request of a connection. | 11 |

## 20.3 Limits of the protocol

**Table 20.2:** Limits of the protocol and planned improvements.

| ID | Matter | Effect | Possible change | Chapter |
|---|---|---|---|---|
| P1 | The check for a `uuid` that is already stored looks at all messages, whatever their destination. | A caller that reuses a `uuid` of another message has its own message dropped, while the call reports success. | Limit the check to messages for the same destination. | 9 |
| P2 | The check works only while the earlier copy is stored. Confirmed messages are deleted at the next cleanup run by default. | A copy that arrives later is stored again. The recipient must recognize it by its `uuid`. | Keep the `uuid` values of confirmed messages for some time. | 9 |
| P3 | A call with several payloads has no `uuid` values from the caller. | If a client repeats such a call after a lost answer, all its messages are stored twice. | Let clients give one `uuid` per payload. | 9 |
| P4 | A message keeps the same `uuid` on its whole path. | Nodes that work together can recognize the message (Chapter 19). | Use a new `uuid` for each hop, and keep the mapping only on the node. | 9, 19 |
| P5 | Calls from the node to a client always count as delivered. The node does not wait for an answer. | A message that is lost on the way stays pending and reaches the client with its next `Pull2`. | Accepted for now. | 7, 9 |
| P6 | Sign-in fails with one general error, whatever the reason. | Clients cannot tell a wrong signature from an expired challenge. This is intended, so that the answer gives nothing away. | None planned. | 8 |
| P7 | Any party that keeps a signed-in connection open and sends a vertex can become a neighbor of a node. | The map can list an entry that is not a real relay. | A rule that tells relays from clients, for example a list of accepted peers. | 10 |
| P8 | There is no limit on the number of messages stored for one address. | Storage can be filled by a caller, within the retention period. | A limit per destination. | 9, 19 |
| P9 | The size of an onion shows how many layers are left. | See Chapter 19. | Padding to a common size. | 5, 19 |
| P10 | Key wrapping and signatures use PKCS#1 v1.5 padding. | See Chapter 19. | OAEP and PSS, in a new version of the native library and of the protocol. | 5, 19 |

## 20.4 Operation

**Table 20.3:** Limits in operation.

| ID | Matter | Effect | Possible change | Chapter |
|---|---|---|---|---|
| O1 | The dashboard has no sign-in. | It is protected only by who can reach the control endpoint. | A sign-in for the dashboard. | 14, 16 |
| O2 | Static files and HTTP methods that a rule does not list are never blocked by a blacklist. | Operators may expect a rule to cover more than it does. | Documented in Chapter 14. | 14 |
| O3 | A file given with `--config` overrides command-line arguments. | Surprising for operators who expect the usual order. | Documented in Chapters 4 and 15. | 15 |
| O4 | After a change of `Hostname` or `OnionService` while the node runs, the node's vertex keeps the old value until it is signed again. | Different places report different hostnames for a while. | Restart the node after such a change. | 15 |
| O5 | The node offers plain HTTP only and has no rate limits. | TLS and limits must be provided in front of the node. | Documented in Chapter 19. | 19 |
| O6 | The development configuration uses the `Debug` log limit. | The log holds routing data and the keys of callers. | Use the `Warning` limit outside development. | 18 |
| O7 | A dashboard page that was open during a restart of the node must be loaded again; the log gets one antiforgery error. | A page that seems to work but does not react. | Keep the keys that protect the page's form data across restarts. | 16 |
| O8 | A list in `/etc/aenigma/appsettings.json` is combined with the default list by position, not replaced. | An own `HttpBlacklists` list can replace a default rule at the same position without a message. | Documented in Appendix A; check the rules after such a change. | 14, A |
| O9 | If two nodes add each other as peers, each hub holds two connections for the same address and only one stays signed in. | One of the two vectors is closed and built again at every five-minute run, with error events; messages are still delivered. | Add a peer on one of the two nodes only. | 11 |
| O10 | A peer that stops without closing its connection is noticed only after the connection times out. A node with a connected peer also takes longer to stop. | The dashboard and the map are behind for that time. | Not examined yet. | 11, 16 |

## 20.5 Technical debt

**Table 20.4:** Code and dependencies that need attention.

| ID | Matter | Possible change | Chapter |
|---|---|---|---|
| T1 | The Azure key source (`AzureClient`, `AzureKeysReader`, `AzurePassphraseReader`, the settings `AzureVaultUrl` and `PassphrasePath`) is only partly implemented. | Planned to be removed. | 6 |
| T2 | Code of the node that only the tests use: `IEnvelopeSealer` with `Seal` and `Factory.CreateSealer`, the factories that take a key instead of a key file (`CreateSigner`, `CreateUnsealer`) and the envelope `Unseal`; `SignatureRequestDto`; `GetPrivateKeyAsync` and the blocking `ReadPrivateKey` and `ReadPublicKey` of the key reader; the views `Authenticated` and `Pending` of `SessionManager`, and the set of signed-in connections behind the first. | Kept on purpose, as long as the tests use them. | 5, 6, 8, 21 |
| T4 | The build reports a known vulnerability in `SQLitePCLRaw.lib.e_sqlite3` 2.1.6, which comes with the Entity Framework Core packages of version 8.0.0. The project itself targets .NET 10. | Upgrade the Entity Framework Core packages, then run the migrations and the federation test again. | 13 |
| T7 | Some parts have no automated test and are checked by hand: connections through Tor, the Debian package with its upgrade and its systemd unit, and the dashboard in a browser. The connection vectors of the bridge, the start-up code and the dashboard components are run only by the integration tests, whose nodes are separate processes, so a coverage report shows them as not covered. | Accepted for now; Section 21.4 lists the manual checks. | 21 |
| T6 | The hub methods `Pull` and `Cleanup` are deprecated. | Remove them when all clients use `Pull2` and `Cleanup2`. The bridge then no longer needs its fallback to `Cleanup`. | 9, 11 |

## 20.6 Work in the client

The Android app is the only client at present. Three changes on its side follow from changes in
the node.

**Table 20.5:** Follow-up work in the client.

| ID | Matter | Chapter |
|---|---|---|
| C1 | Confirm messages with `Cleanup2` and the `Id` of the last message of a page, instead of `Cleanup`. | 9 |
| C2 | Accept `Pull2` responses of up to about 320 KiB: a page of 20 messages, each up to 16,384 characters. | 9, 15 |
| C3 | Send no `uuid` with a call that has several payloads; such calls are rejected. | 9 |

## 20.7 Summary

Five defects remain open: a file left over when the node stops during an upload or cannot delete
it, a second blacklist for the same endpoint that has no effect, a locked node that does not tell
the nodes connected to it, a closed connection vector that can be started again, and a long wait
for a peer on Tor that cannot be reached. In operation, a dashboard page must be loaded again after
a restart of the node, lists in the settings are combined by position, and two nodes should not add
each other as peers. The limits of the protocol concern the `uuid` of a
message, which is checked too broadly, kept too briefly and the same on the whole path, and the lack
of limits per address. The technical debt consists of the unfinished Azure key source, code of
the node that only the tests use, an outdated database package, the deprecated `Pull` and `Cleanup`,
and the parts that are still checked by hand.
