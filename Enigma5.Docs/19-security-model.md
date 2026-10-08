# 19. Security model

**Abstract.** The earlier chapters describe the security mechanisms of a node one by one. This
chapter puts them together. It states what the system protects, which parties take part and what
each of them is trusted with, and what each party can learn. It then lists the mechanisms with the
property each one provides, and names the properties that the system does not provide. It ends with
the duties of a node operator. The chapter describes the design as it is; open problems are listed
in Chapter 20.

## 19.1 What is protected

**Table 19.1:** Protected goods.

| Good | Meaning |
|---|---|
| Content of messages | Only the recipient can read a message. |
| The path of a message | No single node learns both who sent a message and who receives it. |
| The node's private key | Only the node can remove its layer from an onion and sign its vertex. |
| The network map | A vertex can be published and changed only by the node it describes. |
| Stored data | Messages, shared data and files are kept no longer than needed. |
| Availability | A node keeps working when it receives wrong or oversized input. |

The system stores no accounts, names, phone numbers or other personal data. An identity is a key
pair, and an address is the hash of a public key (Chapter 5).

## 19.2 Parties and trust

**Table 19.2:** Parties and what is assumed about them.

| Party | Assumption |
|---|---|
| Sender and recipient | Keep their private keys secret. They are not trusted by the node: every input from a client is checked. |
| A relay node | Follows the protocol for the messages it handles, but may look at everything it sees. It is not trusted with content or with the full path. |
| A peer node | Like any caller, plus the right to be listed as a neighbor while it is connected (Chapter 10). |
| The operator | Fully trusted on the own node: holds the key, sees the database and the log, controls the configuration. |
| Other programs and users on the node's machine | Not trusted, but only weakly kept out (Section 19.6). |
| An observer of the network | Sees who connects to whom and how much data flows, unless the connection runs through Tor. |

The design aims at a node that is *honest but curious*. A single such node cannot read a message
or link its sender to its recipient. The design does not protect the path against several nodes
that work together, or against an observer who watches the whole network (Section 19.5).

## 19.3 What each party can learn

**Table 19.3:** What a party learns about one message.

| Party | Learns | Does not learn |
|---|---|---|
| First node on the path | The address the sender signed in with; the next address; size, time and `uuid` | Content; the later stops; the recipient |
| A middle node | The caller it got the onion from (a peer node); the next address; size, time and `uuid` | Content; the sender; the recipient |
| Last node on the path | The recipient's address; the caller before it; size, time and `uuid` | Content; the sender |
| Recipient | Content, and whatever the sender put into it | The path, unless the content tells it |
| Operator of a node | Everything that node learns, and what is stored in its database and log | — |

A node learns the next address because it must pass the onion on. It cannot tell whether the next
address is another node or the recipient, except by looking it up in the network map.

## 19.4 Mechanisms

**Table 19.4:** Security mechanisms and the property each one provides.

| Mechanism | Property | Chapter |
|---|---|---|
| Onion encryption: one layer per node, each a hybrid of RSA and AES-256-GCM | A node can remove only its own layer. Content and later stops stay hidden. A changed layer is detected and rejected. | 5 |
| Addresses as hashes of public keys | An address cannot be claimed without the matching key. | 5 |
| One strict reading of a public key, used for the address and, written out again, for every signature check | The address of a caller and the key that checks its signature are always the same key. | 5 |
| Sign-in by signing a random challenge | A caller proves control of an address before it can collect or route messages. | 8 |
| One-time challenges, created by a secure random source | A recorded sign-in cannot be replayed. | 8 |
| Messages are returned only to the signed-in address | A caller can collect and confirm only its own messages. | 9 |
| Confirmation limited to what the connection has pulled | A caller cannot make the node drop messages it never received. | 9 |
| Signed vertices, with checks of key, address, age and signature | Nobody can publish or alter the vertex of another node. Old vertices cannot be replayed for long. | 10 |
| Neighbors only with a live session | A node lists only parties that are connected to it right now. | 10 |
| Sign-in on behalf of a peer only with the node's own key | Only the node's own bridge can act for a peer address. | 8, 11 |
| Relay rules for `RouteMessage` | A neighbor sends one message per call with its `uuid`, so relayed messages stay traceable and are not stored twice. | 9 |
| Size checks before every native call | Wrong lengths cannot make the native library read outside its buffers. | 5 |
| Limits on onion size, batch size, hub message size and request size | One request cannot use unbounded memory. | 9, 12, 15 |
| Signed shared data | A reader can check who stored a shared item, and the node rejects items with a wrong signature. | 12 |
| Tags as random GUIDs; file paths built from the parsed GUID only | Stored items cannot be listed or guessed, and a tag cannot point outside the upload directory. | 12 |
| Access counts and retention periods | Stored items disappear after use or after a fixed time. | 12, 17 |
| Two endpoints and blacklists | Operator functions are not offered on the public endpoint. | 14 |
| Passphrase in the kernel keyring | The passphrase is not kept in a file, and it can be removed at any time to lock the key. | 6 |
| All database writes through one writer | Counters and duplicate checks stay correct under load. | 13 |

## 19.5 What the system does not provide

These are properties of the design, not defects of the code. A reader who judges the system must
know them.

- **No protection against nodes that work together.** A message keeps the same `uuid` on its whole
  path (Chapter 9). Two nodes on the path that compare their records can see that they handled the
  same message. If the first and the last node do so, they link the sender's address to the
  recipient's.
- **Size shows the position on the path.** Each layer adds a fixed number of bytes (574 with
  4096-bit keys). The size of an onion therefore shows roughly how many layers are left.
- **No protection against traffic analysis.** A node passes a message on at once. It adds no delay,
  no padding to a common size and no cover traffic. An observer who sees the traffic into and out
  of a node can match messages by time and size.
- **No forward secrecy.** Layers are encrypted with long-lived RSA keys. If the private key of a
  node becomes known later, its layer can be removed from onions that were recorded earlier. If
  the recipient's key becomes known, the content can be read.
- **Older RSA padding.** Key wrapping and signatures use PKCS#1 v1.5, the default of OpenSSL, and
  not OAEP and PSS.
- **Sign-in proves a key, not a person.** Anyone can create a key pair and sign in. The node cannot
  keep a party out, and cannot limit one party by address, because a new address costs nothing.
- **No limits per caller.** The node does not limit the rate of calls, the number of connections, or
  the number of messages stored for one address. Removing a layer costs one RSA private-key
  operation (Chapter 5). Limits of this kind must be set in front of the node, for example in a
  reverse proxy.
- **No transport encryption in the node.** Kestrel listens on plain HTTP. Onions are encrypted end
  to end, but the sign-in exchange, the network map, shared data, files and all traffic metadata
  cross the connection as they are. Protection on the wire comes from a Tor onion service or from
  a reverse proxy with TLS in front of the public endpoint.
- **Files and shared data are stored as they arrive.** The node does not encrypt or inspect them.
  A client that wants them private must encrypt them before the upload.
- **The network map is public.** Addresses, hostnames and neighbor lists of all nodes can be read
  by anyone through the HTTP API.

## 19.6 The node's own secrets

**The private key.** It is a PEM file on disk. A key that the node creates at its first start has
no passphrase, so that a new node works at once; encrypting or replacing it is the operator's task
(Chapter 6). Whoever can read an unencrypted key file can act as the node. The file must be
readable by the node's user only.

**The passphrase.** While the key is unlocked, the passphrase is held in the kernel keyring. In
`Persistent` mode it stays there across restarts for up to three days; in `Ephemeral` mode it is
gone when the process ends (Chapter 6). The keyring belongs to the node's user. Other processes of
the same user can reach it.

**The dashboard.** It can unlock and lock the key and change the peers, and it has no sign-in. Its
only protection is that the control endpoint cannot be reached by others (Chapter 14). The
passphrase is typed in a browser and sent to the node over that connection.

**The log.** At the `Debug` limit, the log holds routing data of every message and the keys and
signatures of callers (Chapter 18). The passphrase is not logged. A node with real users must run
with the `Warning` limit or a higher one, as the Debian package does.

**The database.** It holds the pending messages with their next address, in encrypted form, and
the peers. It needs the same file protection as the key.

## 19.7 Duties of an operator

1. Run the node from a package, or with the `Warning` log limit set by hand. Never use the `Debug`
   limit on a node with real users.
2. Encrypt the private key with a passphrase (`aenigma-lock-key`, Appendix A) and choose the
   passphrase mode that fits the machine (Chapter 6).
3. Keep the control endpoint on the loopback address. Open the dashboard only from the machine
   itself, or through a protected channel such as an onion service or an SSH tunnel.
4. Publish the public endpoint only through a Tor onion service or a reverse proxy with TLS, and
   set rate and connection limits there.
5. Keep the default blacklist rules, and write blacklist endpoints as IP addresses (Chapter 14).
6. Restrict the key files, the database, the upload directory and the log directory to the node's
   user.
7. Keep the retention periods short. They decide how long routing data stays on disk.
8. Add as peers only nodes whose operators are known, and check the address of a peer through a
   separate channel before adding it.

## 19.8 Summary

The system protects the content of a message from every node, and its path from any single node. It
does so with layered encryption, addresses bound to keys, signed-in connections and a signed
network map, and it checks every input before use. It does not protect the path against nodes that
work together or against an observer of the whole network: messages keep their `uuid` and their
timing, and their size shows the remaining layers. It has no forward secrecy and no limits per
caller. A node's own security rests on its operator: the private key, the passphrase, the
dashboard, the database and the log are protected by file permissions, by keeping the control
endpoint private, and by a log limit that keeps routing data and secrets out of the log.
