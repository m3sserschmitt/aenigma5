# 18. Logging

**Abstract.** A node writes structured log events with the library Serilog. This chapter describes
where the events go, which levels the code uses, and which parts of the code write most of them. It
then states plainly what the events contain at each level, because at the `Debug` level they hold
data that should not be kept on a node that carries private messages. It ends with the names of the
log properties and with rules for contributors. [`README.md`](../README.md) shows the default
`Serilog` section of the configuration.

## 18.1 Setup

`Enigma5.App/App.cs` sets up Serilog when the host is built (Chapter 4). The setup has two parts:

- everything in the `Serilog` section of the configuration is applied (`ReadFrom.Configuration`);
- output to the console is always added in code (`WriteTo.Console`).

The code logs through the standard interface `ILogger<T>` of .NET. Serilog receives these events,
so no class depends on Serilog itself. `T` is the class that writes the event; its full name is
stored in the event as `SourceContext`.

## 18.2 Where the events go

**Table 18.1:** Log outputs.

| Output | Format | Defined in |
|---|---|---|
| Console | One line of text per event: time, level, message | Code; cannot be turned off by configuration |
| File | One JSON object per line (compact JSON format of Serilog) | The `WriteTo` list in the configuration |

The default configuration writes the file to `logs/logs-<date>.txt`, relative to the current
working directory, and starts a new file each day. The Debian package writes to
`/var/log/aenigma/` (Appendix A). The file output of Serilog keeps the 31 newest files and deletes
older ones, unless the configuration sets another number.

When the node runs as a service, the console output goes to the journal of the service manager.
The level limits of Section 18.3 apply to both outputs.

## 18.3 Levels

The code uses few levels.

**Table 18.2:** Use of log levels in the node's code.

| Level | Used for |
|---|---|
| `Debug` | The normal course of events: every hub call, every MediatR command and its result, every step of the network bridge. |
| `Warning` | A blacklist that can never match (Chapter 14); a setting whose value cannot be read (Chapter 15); the private key becoming unusable or usable again (Chapter 11). |
| `Error` | Exceptions, and failed steps of the bridge and of the key setup. |
| `Critical` | Key files that cannot be read, and database settings that cannot be applied (Chapter 13). |

The node's code writes nothing at the `Information` level. Events at that level come from the
libraries, for example from Hangfire.

The configuration sets the lowest level that is written (`MinimumLevel:Default`) and can set other
limits for parts of the code by name (`MinimumLevel:Override`).

**Table 18.3:** Default level limits.

| Configuration | Default limit | Limits for ASP.NET Core |
|---|---|---|
| `Enigma5.App/appsettings.json` (development) | `Debug` | `Warning`; `Error` for request diagnostics |
| Debian package | `Warning` | The same |

The difference is large. At the `Debug` limit almost every event the node writes is a `Debug` event,
several for each call. At the `Warning` limit the node writes only warnings and failures, and none of
the events that describe normal calls.

## 18.4 What writes the events

Three places write most events.

**The hub log filter.** `LogFilter` is the first filter of the hub pipeline (Chapter 7). It writes
one `Debug` event before each hub call, with the method name, the connection and the arguments, and
one after it, with success or the list of errors. If a call throws an exception or returns nothing,
it writes an `Error` event, again with the arguments.

**The command log behavior.** `RequestResponseLoggingBehavior` wraps every MediatR command and
query (Chapter 2). It writes one `Debug` event with the command before the handler runs, and one
with the command and its result after it. If the handler throws an exception, it writes an `Error`
event with the command and returns a failed result.

**The components.** The network bridge and its connection vectors log each step (Chapter 11). The
other filters log their decisions. The single-thread runner logs every exception of a work item
(Chapter 3). Hangfire logs its own activity, which is frequent at `Debug`.

## 18.5 What the events contain

The two wrappers of Section 18.4 log whole objects: the arguments of a hub call and the properties
of a command and of its result are written to the event as structured data. This makes the log very
useful for finding errors. It also means that the log holds whatever these objects hold.

**Table 18.4:** Data in the log at the `Debug` level.

| Data | Written by | Why it matters |
|---|---|---|
| Public keys and signatures of callers at sign-in | Hub log (`Authenticate`) | Shows which address used which connection, and when. |
| Onions as received, and the next address and remaining onion after a layer was removed | Hub log (`RouteMessage`), command log (`CreatePendingMessageCommand`) | Shows which caller sent a message to which next stop, and when. A node is meant to forget this. |
| Pending messages returned to a recipient | Command log (result of the pending messages query) | The same, for the receiving side. |
| Vertices, shared data and peers | Hub log, command log | Mostly public data. |

The events of the `Error` level can contain the same data for the one call that failed, because
`LogFilter` and the command log behavior add the arguments to their error events.

The consequences for operators are:

- A node that serves real users must not run with the `Debug` limit. The limit of the Debian
  package, `Warning`, is the right one. The `Debug` limit of the development configuration is meant
  for a developer's own machine.
- Log files written with the `Debug` limit must be treated as secret, and deleted when they are no
  longer needed.
- The console output has the same content. On a service it ends up in the system journal.

The passphrase of the private key is not written to the log. `SetMasterPassphraseCommand` carries
it, but keeps it in a private field and hands it out through the method `GetPassphrase()`. The
wrappers log public properties only, so the event of this command shows its type and nothing else. Versions of the node before 5.0.0 exposed the
passphrase as a property and wrote it to the log when it was entered on the dashboard. `Debug` log
files of such versions should be deleted.

Chapter 19 places the content of the log in the security model.

## 18.6 Property names

Events are written with message templates. A name in braces becomes a property of the event, which
can be searched in the JSON file. The names used by the node are constants in the class
`Constants.Serilog` (`Enigma5.App.Common/Constants.cs`).

**Table 18.5:** Log properties.

| Property | Content |
|---|---|
| `HubMethodName` | Name of the hub method. |
| `HubMethodArguments` | Arguments of the hub call, as structured data. |
| `HubMethodInvocationErrors` | Errors returned by a hub call. |
| `ConnectionId` | SignalR connection of the caller. |
| `DestinationConnectionId` | Connection a message was passed on to. |
| `Address` | An address. |
| `Command`, `CommandResult` | A MediatR command and its result, as structured data. |
| `BridgeMethodName`<br>`HubConnectionsProxyMethodName`<br>`ConnectionVectorMethodName` | Step of the network bridge that wrote the event. |
| `ConnectionVector` | The peer connection the event is about. |

Log files written by versions before 5.0.0 use two other names: `CommendResult` for the result of
a command, and `ConnectionIdKey` for the connection in the first event of `LogFilter`. Searches in
such files must use the old names.

An `@` before a name, as in `{@Command}`, tells Serilog to store the object with all its public
properties. Without the `@`, only the text form of the value is stored.

## 18.7 Reading the log

Each line of the file is a JSON object. `@t` is the time in UTC, `@mt` the message template, `@l`
the level (left out for `Information`), and `@x` the exception, if any. The other fields are the
properties.

**Listing 18.1:** Examples of searching the log file with `jq`.

```bash
# all errors
jq -c 'select(."@l" == "Error" or ."@l" == "Fatal")' logs/logs-20261001.txt

# all events of one hub method
jq -c 'select(.HubMethodName == "Pull2")' logs/logs-20261001.txt

# all commands that did not succeed
jq -c 'select(.CommandResult.Success == false) | .Command' logs/logs-20261001.txt
```

## 18.8 Rules for contributors

- Log through `ILogger<T>` and with message templates. Use the property names of
  `Constants.Serilog`, and add a new constant for a new property.
- Use `Debug` for the normal course of events, `Warning` for a setup that is probably a mistake,
  and `Error` for failures. A `Warning` event is written on packaged nodes, so it must not carry
  data about calls or callers.
- Do not put secrets into objects that the two wrappers log. A command that carries a secret must
  not expose it as a public property. `SetMasterPassphraseCommand` shows the pattern: a private
  field and a method that returns it.
- Use `@` only for objects whose whole content may appear in the log.

## 18.9 Summary

The node logs through `ILogger<T>` into Serilog, which writes text to the console and JSON to a
daily file. The code uses `Debug` for normal events and `Error` or `Critical` for failures. A hub
filter and a MediatR behavior log every hub call and every command with their full content. At the
`Debug` limit of the development configuration, the log therefore holds routing data and the keys
and signatures of callers. The passphrase of the private key is kept out of the log, because its
command does not expose it as a property. The Debian package uses the `Warning` limit; a node with
real users must not go below it.
