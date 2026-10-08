# 1. Introduction

**Abstract.** This chapter explains what Aenigma is and why it exists, without looking at the
code. It describes the problem the system solves, the goals that follow from it, the people and
systems involved, and the services a node offers. It then shows how a message travels through the
network, first in plain words and then in a short notation that later chapters build on. The
chapter ends with the scope of this document and a list of the terms it uses.

## 1.1 Background and motivation

In most messaging services, one company runs all the servers. Even when messages are encrypted
end to end, that company can still see the *metadata*: who talks to whom, when, how often and
from where. Metadata alone reveals a lot about people. It also gives one company control over
all communication, and makes that company a single point of failure and a single target for
surveillance.

Aenigma is a federated messaging system built to avoid these problems. No server can read the
messages it handles. Each server only knows where a message came from and where it goes next.
And no single organization runs the network.

## 1.2 System overview

The system has two kinds of software:

- **Nodes** are servers that pass messages on and store them for a while. The software in this
  repository, `Enigma5.App`, is a node. Anyone can run one.
- **Clients** are the apps people use to send and receive messages. Today, the only client is
  the Aenigma Android app.

Each node is run by its own *operator*, who decides which other nodes it connects to. Connected
nodes form a network and pass messages to each other, so users of different nodes can still reach
each other. This way of letting separately run servers work together as one service is called
*federation*.

## 1.3 Design goals

The design has five goals.

- **G1: Private content.** No node can read the content of the messages it passes on or stores.
- **G2: Unlinkable paths.** A node only learns who handed it a message and who it must pass the
  message to. When a message goes through two or more nodes, no single node knows both the sender
  and the recipient.
- **G3: No central operator.** Anyone can run a node. A node joins the network by connecting to
  peers that its operator chooses.
- **G4: No personal data.** Users and nodes are identified only by cryptographic keys. Nodes keep
  no user accounts and require no registration. A client only has to prove that it holds a
  private key.
- **G5: Delivery to offline users.** If the recipient of a message is not connected, the message
  is kept until the recipient connects, for a limited time (14 days by default).

G1 and G2 are achieved with *onion routing*. A message is wrapped in several layers of
encryption, one for each node on its path, and each node can remove only its own layer. In
addition, a node can run as a Tor onion service. It then needs no public IP address or domain
name, and its location stays hidden.

## 1.4 Roles

**Table 1.1:** Roles of the people and systems involved.

| Role | Description |
|---|---|
| User | Sends and receives messages with a client. A user's identity is an RSA key pair created on their device. The user's *address* is the SHA-256 hash of the public key; others use it to reach the user. |
| Node operator | Installs and runs a node, protects its private key, chooses the peers it connects to and manages it through the node's web dashboard. |
| Peer node | A node, run by someone else, that a node has been set up to connect to. Peers pass messages to each other and share information about the network. |
| Contributor | Develops the Aenigma software. This document is written mainly for contributors. |

## 1.5 Services provided by a node

A node offers six services.

1. **Message relay and mailbox.** A node accepts onions from clients and from peer nodes. It
   removes its own layer and passes the rest on to the next party on the path. If that party is
   connected, the message is delivered at once. In every case, the message is also stored until
   the party collects it (*pulls* it) and confirms that it received it.
2. **Federation.** A node keeps connections to the peers its operator has chosen. It passes
   messages to and from them, and shares with them information about how the network is built.
3. **Network map.** A node keeps a map of all the nodes it knows: their addresses, public keys
   and connections. It publishes this map through its API. Each entry is signed by the node it
   describes, so the nodes that pass it on cannot fake it. The map gives a client what it needs to
   build an onion: the addresses and public keys of the nodes on a path.
4. **Temporary shared data.** A client can store a small signed piece of data on a node (up to
   16 KiB by default) and get a link to it. The data can be read a limited number of times and
   expires after 14 days by default.
5. **Temporary file sharing.** A client can upload a file (up to 64 MiB by default) and get a link
   to it. The file can be downloaded a limited number of times and is deleted after 3 days by
   default.
6. **Operator dashboard.** A web page on a separate control endpoint. The default configuration
   blocks it on the public endpoint. On the dashboard, the operator unlocks the node's private
   key, manages peers and finds a QR code that clients can scan to use the node.

## 1.6 Delivery of a message

### 1.6.1 Description in plain words

Alice wants to send a message to Bob, whose client uses some node of the network. Alice's client
is assumed to know Bob's address and the node Bob uses. How users share this information is up to
the client app and is not covered by this document.

1. Alice's client picks a path through the network that ends at Bob's node. It uses the network
   map published by the nodes.
2. The client wraps the message in layers of encryption, one for each node on the path, starting
   with the innermost layer. Only the node a layer was made for can remove it. Removing a layer
   reveals only the next stop.
3. The client sends the onion to the first node on the path. That node removes its layer, learns
   the next stop and passes the rest on. Nodes that are peers pass it directly to each other.
4. Bob's node removes the last layer and learns that the message is for Bob's address. It stores
   the message and, if Bob's client is connected, delivers it at once.
5. Bob's client gets the message at once, or the next time it connects and pulls its messages.
   It then confirms delivery. The node deletes delivered messages after a configurable time.

### 1.6.2 Notation

The same steps can be written in a short notation. For a party `x`, `a(x)` is its address,
`pk(x)` its public key and `sk(x)` its private key. `Seal(pk, d)` encrypts data `d` so that only
the owner of the private key that matches `pk` can read it. `Unseal(sk, ·)` reverses it. The
symbol `‖` joins two pieces of data.

**Listing 1.1:** How an onion is built and processed for a path `(n_1, ..., n_k)` to a
recipient `r`.

```text
Given:   path P = (n_1, ..., n_k), recipient r, payload m

Built by the sender (innermost layer first):
    O_k = Seal(pk(n_k), a(r)       ‖ m)
    O_i = Seal(pk(n_i), a(n_(i+1)) ‖ O_(i+1))        for i = k-1, ..., 1
    the sender sends O_1 to n_1

Processed by node n_i:
    a_next ‖ O' = Unseal(sk(n_i), O_i)
    store O' for a_next; deliver O' to a_next at once if a_next is connected

    where   for i < k:  a_next = a(n_(i+1)),  O' = O_(i+1)
            for i = k:  a_next = a(r),        O' = m
```

Listing 1.1 shows what each node can see. Node `n_i` sees only who sent it `O_i` (the sender if
`i = 1`, otherwise `n_(i-1)`), the next address `a_next` and the length of the rest. No node can
read the payload `m`. Chapters 5 and 9 give the exact byte format of a layer and the exact steps a
node follows.

## 1.7 Deployment

A node is a single Linux service with everything it needs included. It runs on `amd64` and `arm64`
machines. There are three ways to deploy it:

- a Debian package, which also installs command-line tools for managing keys, Tor onion services,
  reverse proxies and VPNs;
- ready-made virtual machine images for VirtualBox and QEMU/libvirt, which set themselves up on
  first boot, including onion services for the node and its dashboard;
- building and running it from source, as described in [`README.md`](../README.md).

Every node has two HTTP endpoints: a *public endpoint* for clients and peer nodes, and a *control
endpoint* for the dashboard and for calls the node makes to itself. Packages and images are
described in [Appendix A](appendix-a-scripts.md).

## 1.8 Scope and organization of this document

This document describes the node software in this repository: the program `Enigma5.App` and the
libraries `Enigma5.App.Common`, `Enigma5.App.Models`, `Enigma5.Crypto`, `Enigma5.Security` and
`Enigma5.Structures`. The native encryption library `Libaenigma7` is described only as far as the
node depends on it. The Android client is described only through what a node expects from
clients. The test projects are described only in Chapter 21, which explains how they are laid out
and how they are run.

This document adds to the reference material in the repository root and does not repeat it:
[`README.md`](../README.md) (installation and settings), [`API.md`](../API.md) (HTTP endpoints) and
[`SIGNALR_API.md`](../SIGNALR_API.md) (hub methods).

Chapters 1 and 2 need no knowledge of the code. Chapters 3 to 18 each cover one technical subject:
concurrency, the life of the process, encryption, key management, the hub pipeline, sign-in,
message routing, the network graph, federation, the HTTP API, the database, access control,
configuration, the dashboard, background jobs and logging. Chapter 19 describes the security
model, Chapter 20 the known issues and Chapter 21 the practices for contributors. Appendix A
describes packaging and deployment.

Some parts of the code are planned for removal: the Azure Key Vault integration, the deprecated
hub method `Pull` and some unused types. They are described where relevant and marked as such.

## 1.9 Terminology

**Table 1.2:** Terms used in this document.

| Term | Meaning |
|---|---|
| Address | Lowercase hexadecimal SHA-256 hash (64 characters) of a public key. It identifies a user or a node. |
| Broadcast | Message a node sends to its neighbors when the network graph changes. |
| Client | App used by people to connect to a node. Clients are never part of the network graph. |
| Connection vector | The pair of connections the network bridge keeps for one peer: one to the local node and one to the peer (Chapter 11). |
| Control endpoint | The second HTTP endpoint of a node (`HttpControl`, port 8081 by default). The dashboard and the node itself use it. |
| Dashboard | Web page for the operator, served on the control endpoint. |
| Federation | Separately run nodes working together and passing each other's messages. |
| Hub | SignalR endpoint (`/OnionRouting`) through which clients and peer nodes exchange messages in real time. |
| Local vertex | The node's own signed entry in the network graph. |
| Neighbor | A node directly connected to a given node in the network graph. |
| Neighborhood | The signed part of a vertex: the node's address, how to reach it, and its neighbors. |
| Network graph | A node's in-memory map of all known nodes and their connections. Also called the *ledger*. |
| Next hop | The address a node learns when it removes its layer of an onion: the next stop of the message. |
| Node | A running instance of `Enigma5.App`. |
| Nonce | Random challenge a node gives a client during sign-in. The client must sign it. |
| Onion | A message wrapped in several layers of encryption, one for each node on its path. |
| Onion service | A service reached through the Tor network at a `.onion` address, without revealing the server's IP address. |
| Peer | A node that a node has been set up to connect to. |
| Pending message | A message stored on a node until its next hop pulls it. |
| Public endpoint | The main HTTP endpoint of a node (`Http`, port 8080 by default), used by clients and peers. |
| Shared data | A small signed piece of data stored for a while on a node and read by its tag. |
| Tag | GUID that identifies a shared data item or an uploaded file. |
| Vertex | The entry of one node in the network graph: its public key, neighborhood and signature. |

## 1.10 Summary

Aenigma is a federated messaging system. Separately run nodes pass onion-encrypted messages on
behalf of clients. Its goals are private content, unlinkable paths, no central operator, no
personal data and delivery to offline users. A node passes on and stores messages, connects to
peers, publishes a signed map of the network, shares data and files for a limited time and offers
a dashboard to its operator. Each node on the path of a message removes one layer of encryption
and learns only the next stop. The rest of this document explains how the node software does
this.
