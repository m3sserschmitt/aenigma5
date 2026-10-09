# 2. Solution architecture

**Abstract.** This chapter describes how a node is built. It lists the outside parties and local
resources a node works with, the six projects that make up the node and how they depend on each other,
and how the main program is organized. It then describes the design patterns used across the
code, the third-party libraries, how long the main components live, and where all data is kept.
Later chapters cover each part in detail.

## 2.1 System context

A node works with four kinds of outside parties and uses several local resources. Table 2.1
lists them and how the node uses each one.

**Table 2.1:** Outside parties and local resources of a node.

| Party or resource | How the node works with it |
|---|---|
| Clients | Connect to the public endpoint. Use the SignalR hub to sign in, send onions and pull messages. Use the HTTP API to read the network map and to share data and files. |
| Peer nodes | The node opens outgoing SignalR connections to each configured peer, either directly or through Tor for `.onion` peers. Peers open the same kind of connections to the node. |
| Operator | Uses the dashboard in a web browser, through the control endpoint. On installed packages, the operator also uses the `aenigma-*` command-line tools (Appendix A). |
| Tor | Incoming: a local Tor service can publish the node's endpoints as onion services. This is set up outside the node. Outgoing: connections to `.onion` peers go through Tor's SOCKS5 proxy. |
| SQLite database | Stores pending messages, shared data, records of uploaded files and the list of peers. |
| Upload directory | Stores uploaded files (setting `WebContentDirectory`). |
| Key files | Hold the node's RSA key pair in PEM format. |
| Linux kernel keyring | Holds the passphrase of the private key while the key is unlocked (Chapter 6). |
| `openssl` program | Creates the node's key pair on first start. |
| `libaenigma.so` | Native library that does all encryption, decryption, signing and signature checking (Chapter 5). |
| Azure Key Vault | Optional source of keys and passphrase. Planned for removal. |

## 2.2 Project structure

The solution has six projects that make up the node. It also has one test project for each of
them, an integration test project and two helper projects for the tests; Chapter 21 describes
these. All projects target .NET 10. Table 2.2 lists what each project of the node is for.

**Table 2.2:** Projects of the node and what they do.

| Project | What it does |
|---|---|
| `Enigma5.App.Common` | Shared constants, access to configuration values, string and JSON helpers, QR code creation, and the `SimpleSingleThreadRunner` used to run work one item at a time (Chapter 3). |
| `Enigma5.Crypto` | .NET wrapper around the native library `libaenigma.so`. It loads the library, declares its functions and offers `SealProvider` for encryption, decryption, signing, signature checking and onion handling. It also computes addresses and hashes, and contains the prebuilt native libraries under `runtimes/`. |
| `Enigma5.App.Models` | Data objects sent to and received from clients and peers, the rules to validate them, and the hub interfaces (`IEnigmaHub` and related interfaces). |
| `Enigma5.Security` | Everything about the node's own keys: reading keys, getting the passphrase, creating keys, storing the passphrase in the kernel keyring, and creating signers and decryptors (`CertificateManager`). Also contains the sign-in steps the node uses when it connects to a hub itself. |
| `Enigma5.Structures` | `OnionParser`, which removes one layer of an onion with the node's private key. |
| `Enigma5.App` | The program itself: web host, SignalR hub, HTTP API, request handlers, database, network graph, network bridge, dashboard and background jobs. |

The projects reference each other in a chain, shown in Listing 2.1. Each project can use every
project above it.

**Listing 2.1:** Project references.

```text
Enigma5.App.Common
└── Enigma5.Crypto
    └── Enigma5.App.Models
        └── Enigma5.Security
            └── Enigma5.Structures     (also references Enigma5.Crypto)
                └── Enigma5.App        (references all five projects)
```

Two of these references need a short explanation. `Enigma5.App.Models` needs `Enigma5.Crypto`
because `VertexBroadcastRequestDto` separates the signed data from its signature while it is being
read. And `Enigma5.App.Common` holds the reference to the package
`Microsoft.AspNetCore.SignalR.Client`, which `Enigma5.Security` then uses through that project.

## 2.3 Organization of Enigma5.App

**Table 2.3:** Contents of the `Enigma5.App` project.

| Folder or file | Contents |
|---|---|
| `App.cs` | Entry point; builds the host. |
| `StartupConfiguration.cs` | Registers services, builds the request pipeline, maps endpoints and runs startup tasks (Chapter 4). |
| `Api.cs` | Code behind the HTTP endpoints (Chapter 12). |
| `Hubs/` | The SignalR hub `RoutingHub`, its filters and adapters, and session management (Chapters 7 and 8). |
| `Attributes/` | Marker attributes that turn hub filters on for single hub methods. |
| `Resources/` | MediatR commands, queries and handlers, the result type `CommandResult`, and the database writer. |
| `Data/` | Entity Framework Core context and entities, and the network graph kept in memory (Chapters 10 and 13). |
| `NetworkBridge/` | Connections to peer nodes (Chapter 11). |
| `UI/` | The Blazor Server dashboard (Chapter 16). |
| `Hangfire/` | Connects Hangfire background jobs to MediatR (Chapter 17). |
| `Middlewares/` | The HTTP blacklist middleware (Chapter 14). |
| `Extensions/` | Helpers to register services, and access checks based on configuration. |
| `Migrations/` | Entity Framework Core migrations and a generated SQL script. |
| `wwwroot/` | Static files for the dashboard, and `.well-known/assetlinks.json` for Android App Links. |

## 2.4 Design patterns

### 2.4.1 Thin transport layer

HTTP endpoints and hub methods only check their input. They then pass the real work to a MediatR
request. Each operation is a command or query class in `Resources/Commands` or
`Resources/Queries`, and a class in `Resources/Handlers` carries it out. A handler returns a
`CommandResult<T>`, which holds a value and a success flag. The transport layer turns this result
into an HTTP status code or a hub result. Section 2.4.7 states what the flag and the value mean. A MediatR pipeline step logs every request and turns
unexpected exceptions into failed results.

### 2.4.2 Shared concerns as hub filters

Sign-in checks, access control, input validation and onion handling are written as SignalR hub
filters. A filter only runs for a hub method that carries its marker attribute, for example
`[Authenticated]`. Filters pass data to the hub method by setting properties on the hub object
(Chapter 7).

### 2.4.3 One thread per owner of shared data

Some components own data that many requests change: sessions, the network graph, the key thread,
database writes, the network bridge and the dashboard state. These components do not use locks. Instead, each one
runs all of its operations one after another on its own thread (Chapter 3).

### 2.4.4 One database writer

All writes to the SQLite database go through one component, on one thread. Reads use a normal
`DbContext` created for each request (Chapter 13).

### 2.4.5 Network graph kept only in memory

The network graph is not saved to disk. After each start, the node rebuilds it from its own
vertex and from the broadcasts it receives from peers (Chapter 10).

### 2.4.6 Native encryption

All encryption, decryption, signing and signature checking is done by `libaenigma.so` (Chapter 5).
.NET is used only for SHA-256 hashing and for random numbers, and the `openssl` program creates the
node's key pair.

### 2.4.7 What a handler result means

All handlers use the success flag and the value of `CommandResult<T>` in the same way.

**Table 2.4:** Meaning of a handler result.

| Kind of request | Result |
|---|---|
| Any | A *failure* means that the handler could not do its work: input that the caller should have checked, or an error. It never means that nothing was found. |
| Reads one item | Success with the item, or success without a value if no such item exists. |
| Reads a list | Success with the list, which may be empty. |
| Changes or removes records | Success with the number of records changed; 0 if none matched. |
| Creates an item | Success with the new item. |

HTTP endpoints turn "no value" and "0 records" into status 404, and a failure into 500
(Chapter 12). There is one exception to the first rule. `CreatePendingMessageHandler` answers a
`uuid` that is already stored with a failure that carries the stored message, so that the hub can
accept the call without delivering the message a second time (Chapter 9).

## 2.5 Third-party libraries

**Table 2.5:** Third-party libraries and what they are used for.

| Package | Project | Used for |
|---|---|---|
| ASP.NET Core<br>(SignalR, minimal APIs, Blazor Server, Kestrel) | App | Web host, real-time hub, HTTP API, dashboard |
| `Microsoft.AspNetCore.SignalR.Client` | Security, App | Outgoing hub connections to peers and to the node itself; signing in on such a connection |
| `Microsoft.EntityFrameworkCore.Sqlite` | App | Database access and migrations |
| `MediatR` | App | Sending commands and queries to their handlers; logging step |
| `LinqKit` | App | Building query conditions step by step (`PredicateBuilder`) |
| `Hangfire.AspNetCore`<br>`Hangfire.InMemory` | App | Repeating and one-time background jobs, stored in memory |
| `Serilog.AspNetCore` | App | Structured logs to the console and to daily JSON files |
| `Microsoft.AspNetCore.OpenApi`<br>`Swashbuckle.AspNetCore.SwaggerUI` | App | OpenAPI document and Swagger UI, only in the Development environment |
| `jsoncanonicalizer` | Common | Canonical JSON (RFC 8785) for signed network graph data |
| `QRCoder` | Common | QR code on the dashboard |
| `Microsoft.Extensions.Configuration`<br>`Microsoft.Extensions.Configuration.Binder` | Common | Typed access to configuration values |
| `Microsoft.Extensions.Logging.Abstractions` | Common | The logging interface used by the single-thread runner |
| `Azure.Identity`<br>`Azure.Security.KeyVault.Secrets` | Security | Access to Azure Key Vault (planned for removal) |

Each project references the packages it uses itself, and no others.

## 2.6 Component lifetimes

The components in Table 2.6 are registered as singletons. They exist, and keep their data, for as
long as the process runs.

**Table 2.6:** Long-lived components.

| Component | Project | Data it holds |
|---|---|---|
| `CertificateManager` | Security | Access to the node's keys; owns the key thread |
| `SessionManager` | App | Pending sign-in challenges and signed-in connections |
| `ConnectionsMapper` | App | Which connection belongs to which address; owned by `SessionManager` |
| `NetworkGraph` | App | The network graph and the node's own vertex |
| `Bridge`, `HubConnectionsProxy` | App | Connections to peer nodes |
| `SingleThreadDbWriter` (as `IDbWriter`) | App | Database writes, one at a time |
| `DashboardUIState` | App | Data shown on the dashboard, with change events; changed on its own thread |
| `SqlitePragmaInterceptor` | App | Applies SQLite settings to every new connection |

All other components live for a short time:

- `EnigmaDbContext` is scoped. One instance exists per HTTP request, per hub call or per scope
  that the code creates on purpose.
- `OnionParser`, `NetworkGraphValidationPolicy`, `MediatorHangfireBridge`, the key reader and the
  passphrase provider are transient. So is `SimpleSingleThreadRunner`, which means that every
  component that asks for one gets its own thread.
- A new hub object and new filter objects are created for every hub method call. Filters pass
  data to the hub method through properties of the hub object (`ClientAddress`, `Next`,
  `Content`, `Uuid`, `DestinationConnectionId`). These values therefore only exist during one
  call.

## 2.7 Where data is kept

Table 2.7 shows where each kind of data is kept and whether it survives a restart.

**Table 2.7:** Where data is kept.

| Data | Location | Survives a restart |
|---|---|---|
| Pending messages, shared data, file records, peers | SQLite database | Yes |
| Uploaded files | Upload directory | Yes |
| Key pair of the node | Key files (or Azure Key Vault) | Yes |
| Passphrase of the private key | Kernel keyring | Only in `Persistent` mode, until the kernel removes it |
| Sign-in challenges and signed-in connections | `SessionManager` (memory) | No |
| Network graph | `NetworkGraph` (memory) | No |
| Connections to peers | `HubConnectionsProxy` (memory) | No |
| Background job schedules | Hangfire in-memory storage | No; added again at startup and on unlock |
| Dashboard data | `DashboardUIState` (memory) | No |

So after a restart, every client has to connect and sign in again, and the network graph at first
contains only the node's own vertex. Once the private key is unlocked, the network bridge connects
again to the peers stored in the database, and the graph fills up again from the peers'
broadcasts (Chapters 10 and 11).

## 2.8 Summary

A node is made of six projects in a simple chain of dependencies, with the program `Enigma5.App`
at the end. The transport code is kept thin and passes work to MediatR handlers. Shared concerns
of the hub are written as filters that attributes turn on. Data shared between requests is owned
by components that run their work on their own thread. All work with keys is done by a native
library. Data that must last is kept only in the SQLite database, the upload directory, the key
files and, optionally, the kernel keyring. Sessions, the network graph and peer connections live
in memory and are rebuilt after every start.
