# 21. Contributor guide

**Abstract.** This chapter is the starting point for anyone who wants to change the code. It
lists what must be installed, how to build and run a node, and how to run several nodes on one
machine to try out federation. It states the conventions the code follows and the rules that the
earlier chapters set for each part of the system, and points to the chapter that explains each
rule. It describes the automated tests and how they are laid out, and how the parts they do not
reach are checked on running nodes. It ends with a checklist for a change and with how to keep
this documentation up to date.

## 21.1 Before you start

**Table 21.1:** Tools needed for each kind of work.

| Work | Needed |
|---|---|
| Building and running a node | .NET 10 SDK, `openssl` (used by the node to create keys), Linux on `amd64` or `arm64` |
| Rebuilding the native library | The packages listed in [`README.md`](../README.md); the git submodule `Libaenigma7` (Chapter 5) |
| Building the Debian packages | `dpkg-dev`, `debhelper`, `lintian` (Appendix A) |
| Building the virtual machine images | Packer, Vagrant, VirtualBox, `qemu-img` (Appendix A) |
| Building the PDF of this documentation | `pandoc`, `weasyprint` (see `Enigma5.Docs/README.md`) |

The native library `libaenigma.so` is stored in the repository for both processor types, under
`Enigma5.Crypto/runtimes/`. It only has to be rebuilt after a change to the library itself.

Clone the repository with its submodule:

**Listing 21.1:** Getting the sources.

```bash
git clone --recurse-submodules <repository URL>
cd aenigma5
```

## 21.2 Building and running

[`README.md`](../README.md) describes how to build and start a node. In short:

**Listing 21.2:** Building and starting a development node.

```bash
cd Enigma5.App
dotnet build
./run-dev.sh          # environment Development: Swagger UI, OpenAPI and /Jobs as well
```

The node then listens on `http://127.0.0.1:8080` (public) and `http://127.0.0.1:8081` (control).
At its first start it creates an unencrypted key pair in `Enigma5.App/` (Chapter 6), the database
`aenigmaDb.sqlite` and the folder `logs/`. These files are ignored by git and must not be
committed.

The development configuration logs at the `Debug` level. Its log files therefore hold routing data
and the keys of callers (Chapter 18). They are for a developer's own machine only.

## 21.3 Running several nodes on one machine

Most behaviour worth checking needs more than one node: federation, the network map, the bridge.
Several nodes can run from the same build. Each one needs its own ports, database, key pair, upload
directory and log folder, and all of these can be set on the command line. Because the blacklists
name the public endpoint, they must be moved to the new port as well (Chapter 14).

**Listing 21.3:** Starting a second node next to the first one.

```bash
cd Enigma5.App
dotnet run --no-build -- \
  --ConnectionStrings:DbConnectionString="data source=/tmp/nodeB/db.sqlite" \
  --Kestrel:EndPoints:Http:Url=http://127.0.0.1:28080 \
  --Kestrel:EndPoints:HttpControl:Url=http://127.0.0.1:28081 \
  --HttpBlacklists:0:Endpoint=http://127.0.0.1:28080 \
  --HubBlacklists:0:Endpoint=http://127.0.0.1:28080 \
  --PrivateKeyPath=/tmp/nodeB/private-key.pem \
  --PublicKeyPath=/tmp/nodeB/public-key.pem \
  --WebContentDirectory=/tmp/nodeB/web \
  --Serilog:WriteTo:0:Args:path=/tmp/nodeB/logs/log-.txt
```

The folder `/tmp/nodeB` must exist; the node creates the key pair and the database in it. To
connect the two nodes, open the dashboard of one of them and add the other as a peer, with the
host `http://127.0.0.1:28080` and the address shown on the other node's dashboard or in its
`GET /Info` (Chapter 16). One side is enough (Chapter 11).

Two facts help when nodes are started and stopped by a script:

- A file given with `--config` overrides command-line arguments (Chapter 15). A script that uses
  both must not set the same setting in both places.
- A node needs a few seconds before it answers `GET /Info`, and the key setup runs in the
  background after that (Chapter 4). A script should wait for `GET /LocalVertex` to return a signed
  vertex before it relies on the key.

## 21.4 Checking a change

A change is checked in two ways: with the automated tests, and, for the parts they do not reach,
by hand on running nodes.

### 21.4.1 Automated tests

**Listing 21.4:** Running the tests.

```bash
dotnet test enigma5.sln                                   # everything
dotnet test Enigma5.App.Tests                             # one project
dotnet test enigma5.sln --settings coverage.runsettings \
  --collect "XPlat Code Coverage"                         # with code coverage
```

**Table 21.2:** Test projects.

| Project | What it tests |
|---|---|
| `Enigma5.App.Common.Tests`<br>`Enigma5.App.Models.Tests`<br>`Enigma5.Crypto.Tests`<br>`Enigma5.Security.Tests`<br>`Enigma5.Structures.Tests`<br>`Enigma5.App.Tests` | The project of the same name, class by class, inside the test process. |
| `Enigma5.App.IntegrationTests` | Whole nodes. Each test class starts one or two nodes as separate processes, from the build output of `Enigma5.App`, and talks to them over HTTP and SignalR as a client or a peer does. |
| `Enigma5.Tests.Base` | Helpers shared by the test projects: the test keys and other stored test data, a temporary folder, a configuration built from pairs, and a certificate manager with a fixed key. |
| `Enigma5.Tests.DataGenerator` | A small program that creates the stored onions (see below). It is not a test project. |

The tests follow these rules:

- **The layout mirrors the code.** The class `Folder/X.cs` of a project is tested in
  `Folder/XTests.cs` of the matching test project. The integration tests are grouped by behavior
  instead: `Http`, `Session`, `Messaging`, `Graph`, `AccessControl`, `Settings`, `Federation`.
- **Test data is stored, not built.** Keys, signed data and onions are constants in
  `Enigma5.Tests.Base`. Only sign-in signatures and signed vertices are made while a test runs,
  because they depend on a fresh challenge or on the current time.
- **Onions come from the generator.** A node cannot build an onion (Chapter 5). The onions in
  `TestOnions.cs` are created by `Enigma5.Tests.DataGenerator`, which calls the native `SealOnion`.
  After a change to the keys or to the format, create the file again with
  `dotnet run --project Enigma5.Tests.DataGenerator > Enigma5.Tests.Base/TestOnions.cs`.
- **Assertions** use the `Assert` class of xUnit; substitutes are made with NSubstitute.
- **A node inside the test process.** `TestNode` in `Enigma5.App.Tests` registers the services
  exactly as the node does, with its own database file and upload folder and one of the test keys.
  Nothing listens on the network. Handlers, the hub, the filters and the endpoint methods are
  tested against it.
- **Test names** state the expected behavior as a sentence.

Code coverage has one blind spot. The nodes of the integration tests are separate processes, so
the code that only they run is shown as not covered: the connection vectors of the bridge, the
start-up code and the dashboard components (Chapter 20, T7).

### 21.4.2 Checks on running nodes

Three areas have no automated test: connections through Tor, the Debian package with its upgrade
and its systemd unit, and the dashboard in a browser. They are checked by hand. The same means help
to look at any behavior more closely.

**Table 21.3:** Ways to check a change on running nodes.

| What changed | How to check it |
|---|---|
| An HTTP endpoint | `curl` against the public and the control endpoint, including invalid input and blocked paths |
| A hub method | A small client that speaks the SignalR JSON protocol over long polling: `POST /OnionRouting/negotiate`, then send and receive messages separated by the character `0x1E`. The sign-in needs `openssl` to sign the challenge (Chapter 8) |
| Routing | The stored onions of the tests, or new ones from the generator, routed with the client above |
| Federation, the map, the bridge | Two nodes as in Section 21.3, one peered with the other; then compare `GET /LocalVertex` of both and the `Messages` table of both databases. For Tor, two virtual machines built from the images (Appendix A) |
| The dashboard | A browser on the control endpoint; with two nodes for the list of peers (Chapter 16) |
| The package | Install the old package on a test machine, change a setting, upgrade, and compare the settings and the state of the service (Appendix A) |
| Background jobs | Wait for the next five-minute mark, or set short periods in the configuration, and read the log (Chapter 17); in `Development`, the page `/Jobs` |
| Settings | Start with wrong or missing values and look for the warnings (Chapter 15) |

The log is the most useful source when a check fails. At the `Debug` level every hub call and every
command is written with its content (Chapter 18). The JSON file can be searched with `jq`.

## 21.5 Conventions

**Table 21.4:** Conventions of the code.

| Topic | Convention |
|---|---|
| License header | Every source file begins with the GPL header used in the existing files. |
| Line endings | The repository mixes LF and CRLF files. Keep the line endings of a file as they are; do not convert a whole file. |
| Classes | Primary constructors; each parameter is copied into a `private readonly` field with a leading underscore. |
| Namespaces | File-scoped, matching the folder. |
| Requests | One MediatR command or query per operation. The handler has the same name with `Handler` instead of `Command` or `Query`, and returns `CommandResult<T>` (Chapter 2). |
| Handler results | A failure means the work could not be done. Not found is a success without a value, and a change that matched nothing is a success with the count 0 (Section 2.4.7). |
| Tests | A new class gets a test class at the mirrored path; a new behavior of the node as a whole gets an integration test (Section 21.4.1). |
| Hub methods | Thin: the work is done by filters and MediatR handlers. Marker attributes select the filters (Chapter 7). |
| Shared data | Owned by one component with a `SimpleSingleThreadRunner` (Chapter 3). |
| Database writes | Only through `IDbWriter` (Chapter 13). |
| Settings | Read only through the helper methods, with defaults from `Constants.cs` (Chapter 15). |
| Fixed values | In `Enigma5.App.Common/Constants.cs`. |
| Logging | `ILogger<T>` with message templates and the property names of `Constants.Serilog`; no secrets in logged objects (Chapter 18). |
| Comments | Few, and only where the reason for the code is not obvious. They explain why, not what. |
| Unused code | Not added. Code that is no longer called is removed, unless a decision to keep it is recorded in Chapter 20. |

## 21.6 Rules by area

The earlier chapters each end with rules for their area. Table 21.5 collects them.

**Table 21.5:** Where the rules for each kind of change are.

| Change | Rules in |
|---|---|
| Code that touches shared data, or a new component with its own thread | Chapter 3, Section 3.5 (rules R1 to R6) |
| A new hub method | Chapter 7, Section 7.8 |
| A new HTTP endpoint | Chapter 12; blacklist rules in Chapter 14, Section 14.9 |
| A new table, column or write operation | Chapter 13, Section 13.8 |
| A new setting | Chapter 15, Section 15.8 |
| A new part of the dashboard | Chapter 16, Section 16.9 |
| A new background job | Chapter 17, Section 17.9 |
| New log events | Chapter 18, Section 18.8 |
| Anything that handles keys, onions or signatures | Chapters 5, 6 and 19 |

Three rules come up in almost every change and are repeated here:

1. A work item of a single-thread runner never waits for its own runner, and never blocks on a
   task (Chapter 3).
2. A value that depends on a stored value is read and written in one step of the database writer
   (Chapter 13).
3. Every input from outside is checked before it is used, and before it reaches the native library
   (Chapters 5 and 7).

## 21.7 Compatibility

A node talks to clients and to other nodes, which may run older or newer versions. A change must
not break them.

- **Hub methods and HTTP endpoints** are a public interface, described in
  [`SIGNALR_API.md`](../SIGNALR_API.md) and [`API.md`](../API.md). A method that must change gets a
  new name, and the old one is marked as deprecated, as was done with `Pull2` and `Cleanup2`
  (Chapter 9).
- **Calls to peers** must cope with peers that do not have a newer method yet. The bridge's
  fallback from `Cleanup2` to `Cleanup` is the example (Chapter 11).
- **The database** is changed only through new migrations; a released migration is never edited
  (Chapter 13).
- **Signed data** (vertices, shared data) uses canonical JSON (Chapter 5). A change to its fields
  changes every signature and must be coordinated with all nodes and clients.

## 21.8 Checklist for a change

1. The solution builds without new warnings.
2. All tests pass (`dotnet test enigma5.sln`), and the change has tests of its own, including
   invalid input.
3. If it affects Tor, the package or the dashboard, it was also checked by hand, as in
   Section 21.4.2.
4. The log of a manual check contains no new errors.
5. File line endings and license headers are unchanged.
6. [`API.md`](../API.md), [`SIGNALR_API.md`](../SIGNALR_API.md) and [`README.md`](../README.md)
   are updated if the interface or a setting changed.
7. The chapters of this documentation that describe the changed behaviour are updated, and so is
   Chapter 20: a fixed issue is removed there, a new one is added.

## 21.9 Keeping this documentation up to date

This documentation describes the code as it is, including its known problems. It states facts and
rules that follow from the code. It is only useful while that stays true.

- Each chapter covers one subject. A change belongs in the chapter of that subject, and other
  chapters refer to it instead of repeating it.
- The conventions for chapters, sections, tables and listings are listed in
  `Enigma5.Docs/README.md`. Lines in listings stay within 86 characters, so that they fit the page
  in the PDF.
- State what the code does and the rule that follows from it. Results of a single test run or of a
  measurement are left out: they age quickly, and a check that matters belongs in an automated
  test.
- After a change, build the PDF (`Enigma5.Docs/build/build-pdf.sh`) and look at the changed pages.
  Wide tables may need shorter cell texts or line breaks (`<br>`) in long names.

## 21.10 Summary

A node is built with the .NET 10 SDK and runs from source with `run-dev.sh`; several nodes can run
on one machine when each gets its own ports, database, keys and log folder. A change is checked
with the automated tests, which mirror the layout of the code and include integration tests that
start real nodes; Tor, the package and the dashboard are checked by hand on running nodes. The code follows a few strict rules: shared data belongs to one
runner, database writes go through one writer, settings go through helpers, and secrets stay out
of the log. The rules for each area are in the chapter of that area. Public interfaces, peers on
other versions, released migrations and signed data must not be broken. A change is complete when
the reference documents, the affected chapters and Chapter 20 describe it.
