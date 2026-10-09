# 4. Startup and runtime lifecycle

**Abstract.** This chapter follows a node from the moment its process starts until it stops. It
describes how the host is built and in which order configuration sources are read, which services
are registered, how requests pass through the pipeline, and what the two HTTP endpoints are for.
It then explains the tasks that run at startup and how the node's key is set up for each
passphrase source. The node's state is described as a simple model with two states, *locked* and
*unlocked*, together with what the node can do in each state. The chapter ends with what happens
when the node stops and starts again.

## 4.1 Building the host and reading configuration

### 4.1.1 Configuration sources

`Enigma5.App/App.cs` builds a generic host with `Host.CreateDefaultBuilder`. This registers the
configuration sources `S1` to `S4` of Listing 4.1. `App.cs` then adds one more source, `S5`: if a
configuration value named `config` is set, usually with `--config <path>` on the command line, the
JSON file at that path is added, and it is reloaded when it changes.

**Listing 4.1:** Configuration sources, from lowest to highest priority.

```text
S1  appsettings.json in the content root (the current working directory)
S2  appsettings.{Environment}.json
S3  environment variables
S4  command-line arguments
S5  the JSON file named by the value "config"            (only if set)

value(key) = value of key in S_j, for the largest j such that S_j defines key
```

`S5` is added last, so a file given with `--config` overrides every other source, including
command-line arguments. The installed package uses this layering: its default settings are the
`appsettings.json` in the working folder `/usr/lib/aenigma` (`S1`), and
`/etc/aenigma/appsettings.json`, loaded with `--config`, holds only the settings an operator
changed (Appendix A). Lists are layered item by item, by position, not replaced as a whole.

Relative paths in the configuration, such as those of the database, the key files, the upload
directory and the log files, are relative to the current working directory. When `dotnet run` is
started in `Enigma5.App/`, all these files therefore stay in that folder. The installed service
runs in `/usr/lib/aenigma` and uses absolute paths.

### 4.1.2 Host

The host sets up Serilog from the `Serilog` configuration section and adds output to the console
(Chapter 18). It then starts the web host on Kestrel, with `StartupConfiguration` as its startup
class. The default environment is `Production`. The script `Enigma5.App/run-dev.sh` starts the
node in `Development`, which also turns on Swagger UI, the OpenAPI document and the jobs page of
Hangfire (Chapter 17).

## 4.2 Service registration

`StartupConfiguration.ConfigureServices` registers:

- SignalR, with the keep-alive, timeout, handshake and reconnect settings defined in
  `Enigma5.App.Common/Constants.cs`, and the six hub filters in a fixed order (Chapter 7);
- the long-lived components listed in Table 2.6;
- the key reader and the passphrase provider chosen by the settings `KeySource` and
  `PassphraseSource` (Chapter 6);
- Hangfire, with jobs stored in memory, and a Hangfire server (Chapter 17);
- the database context and the database writer for the configured `DbProvider`. Only `Sqlite` is
  supported; any other value stops the startup (Chapter 13);
- MediatR, with all handlers of the `Enigma5.App` project and the logging step;
- Razor components with interactive server rendering, for the dashboard (Chapter 16);
- antiforgery protection, the OpenAPI document generator, and JSON settings that leave `null`
  values out of HTTP responses.

## 4.3 Request pipeline

### 4.3.1 Middleware and endpoints

`StartupConfiguration.Configure` builds the pipeline in this order:

1. Swagger UI (Development only);
2. routing;
3. antiforgery protection;
4. static files from `wwwroot/`;
5. `HttpBlacklistAuthorizationMiddleware`, which rejects requests that match the setting
   `HttpBlacklists` (Chapter 14);
6. the endpoints:
   - the dashboard (`/Dashboard`), a Razor component with interactive server rendering;
   - the SignalR hub at `/OnionRouting`, with stateful reconnect turned on;
   - the HTTP API (Chapter 12 and [`API.md`](../API.md));
   - the jobs page of Hangfire at `/Jobs` (Development only; Chapter 17);
   - the OpenAPI document at `/openapi/v1.json` (Development only).

Static files are served before the blacklist middleware runs, so the blacklist never blocks them.

### 4.3.2 Public and control endpoints

Kestrel listens on the endpoints listed under `Kestrel:EndPoints`. The code refers to two of them
by name. Table 4.1 shows what each one is for.

**Table 4.1:** Endpoints with a specific role.

| Endpoint | Default address | Used by |
|---|---|---|
| `Http` (public) | `http://127.0.0.1:8080` | Clients and peer nodes. The network bridge also connects to the local hub through this endpoint (Chapter 11). |
| `HttpControl` (control) | `http://127.0.0.1:8081` | The dashboard, and the node itself when it calls `TriggerBroadcast` (Chapter 11). |

Both endpoints serve the same application. The only difference between them is what the settings
`HttpBlacklists` and `HubBlacklists` block on each one. The default configuration blocks the
dashboard and `TriggerBroadcast` on the public endpoint. More endpoints can be added and
restricted in the same way (Chapter 14).

## 4.4 Startup tasks

Once the pipeline is built, `Configure` runs five tasks, one after another:

1. **Settings check.** Every typed setting whose value cannot be read is reported with a warning,
   and the same check is registered to run again whenever a configuration file is loaded again
   (Chapter 15).
2. **Database migration.** Pending Entity Framework Core migrations are applied. If this fails,
   the error is logged and the startup stops.
3. **Key setup.** A `SetMasterPassphraseCommand` with an empty passphrase is sent. The node does
   not wait for it: the setup runs in the background while the host finishes starting
   (Section 4.5).
4. **Blacklist check.** Every blacklist whose endpoint can never match is reported with a warning
   (Chapter 14).
5. **Cleanup jobs.** Four repeating Hangfire jobs are registered, each running every five
   minutes. They remove expired and delivered messages, expired shared data, expired files and
   old vertices of the network graph (Chapter 17).

The host then starts accepting connections. So clients can connect before the key setup has
finished.

## 4.5 Key setup

### 4.5.1 Setup at startup

What the key setup does at startup depends on the setting `PassphraseSource` and on whether the
key files exist. The code is in `CertificateManager.SetupAsync` and `SetMasterPassphraseHandler`.
Table 4.2 summarizes it. Chapter 6 covers keys and passphrases in detail.

**Table 4.2:** Key setup at startup, by passphrase source.

| `PassphraseSource` | Passphrase | Private key missing or empty | Private key present |
|---|---|---|---|
| `Dashboard` (default) | None (empty) | A 4096-bit RSA key pair is created with `openssl`, without encryption. The node becomes unlocked. | Nothing is created. The node becomes unlocked if the key is not encrypted, or if its passphrase is still in the kernel keyring. Otherwise it stays locked. |
| `Keyboard` | Typed in on the console | A key pair is created and encrypted with the passphrase. The passphrase is then stored in the keyring. | The passphrase is stored in the keyring. The node becomes unlocked if the passphrase is correct. |
| `Azure` (planned for removal) | Read from Azure Key Vault | Same as `Keyboard`. | Same as `Keyboard`. |

Creating an unencrypted default key is intended. It lets a node work right after installation,
without any setup. Encrypting or replacing that key is the operator's job. The Debian package
creates an empty private key file when it is installed. So an installed node with the default
`Dashboard` source creates such a key when it first starts. The tool `aenigma-lock-key` encrypts
this key, and `aenigma-keys` replaces it with a new encrypted one (Appendix A).

### 4.5.2 Unlocking while the node runs

When the operator enters a passphrase on the dashboard, the same command is sent again, this time
with that passphrase. Missing keys are created and encrypted with it, the passphrase is stored in
the keyring, and the node tries to sign a new local vertex. A wrong passphrase is stored too, but
signing fails, so the node stays locked.

### 4.5.3 What a successful setup does

The setup has succeeded if the node could sign its local vertex. In that case, the handler:

- registers the repeating network bridge job (every five minutes) and queues one run right away,
  which connects to the configured peers (Chapter 11);
- marks the key as unlocked on the dashboard.

## 4.6 Operating states

### 4.6.1 State model

A node is *unlocked* if it can use its private key, and *locked* if it cannot. Listing 4.2 shows
how the node moves between these two states.

**Listing 4.2:** Operating states and how the node moves between them.

```text
States:   LOCKED, UNLOCKED

State after startup:
    UNLOCKED   if the private key is unencrypted or its passphrase is in the keyring
    LOCKED     otherwise

Changes of state:
    LOCKED     --[correct passphrase supplied]-->     UNLOCKED
    LOCKED     --[wrong passphrase supplied]-->       LOCKED
    UNLOCKED   --[operator locks the key]-->          LOCKED     (no effect on an
                                                                  unencrypted key)

A passphrase is supplied by the operator on the dashboard or, at startup, by the
passphrase source (Table 4.2).
```

Locking removes the passphrase from the keyring and makes the node create its local vertex again.
This fails, so the vertex is left without a signature. The node then closes its connections to
peers, so that they stop listing it as a neighbor. The repeating network bridge job stays
registered, but a run on a locked node does nothing (Chapter 11).

### 4.6.2 What the node can do in each state

A locked node still offers every service that does not need its private key. Table 4.3 lists what
works in each state.

**Table 4.3:** What a node can do in each state.

| Capability | Locked | Unlocked |
|---|---|---|
| `GET /Info`, `/Vertices`, `/Vertex`; shared data and file endpoints | Works | Works |
| `GET /LocalVertex` | Works, but the vertex has no signature and `graphVersion` in `/Info` is `null` | Works; the vertex is signed |
| Client sign-in, `Pull2`, `Cleanup2` | Works | Works |
| `RouteMessage` (removing the node's layer of an onion) | Fails with `Could not parse onion.` | Works |
| Connecting to peers and sending broadcasts | Not done; the bridge keeps no connections to peers while the key cannot sign (Chapter 11) | Works |

## 4.7 Stopping and starting again

When the node stops, everything kept in memory is lost: sessions, the network graph, the
connections to peers, the dashboard data and the background job schedules. The database, the
uploaded files, the key files and, in `Persistent` mode, the passphrase in the kernel keyring are
kept.

When the node starts again:

- clients connect and sign in again. SignalR's stateful reconnect only covers short breaks while
  the same process is running;
- the cleanup jobs are registered again straight away;
- the node unlocks itself if the private key is not encrypted, or if its passphrase is still in
  the keyring (`Persistent` mode). In `Ephemeral` mode the operator has to unlock it again;
- once it is unlocked, the network bridge connects again to the peers stored in the database, and
  the network graph is rebuilt from their broadcasts.

The installed service is started, and restarted after a failure, by systemd (`Restart=always`); see
Appendix A.

## 4.8 Summary

The node reads its configuration from five sources, and a file given with `--config` has the
highest priority. At startup, the node registers its services and builds a pipeline in which a
blacklist middleware runs before all endpoints. It then updates the database, starts the key setup
in the background and registers its cleanup jobs. Two endpoints serve the same content and are
used as the public and the control endpoint; only the configuration makes them different. The key
setup depends on the passphrase source and on whether the key files exist, and it leaves the node
either locked or unlocked. A locked node still serves everything that does not need its private
key, but it can neither route onions nor connect to peers. All data kept in memory is lost when
the node stops, and an unlocked node rebuilds its peer connections and network graph after a
restart.
