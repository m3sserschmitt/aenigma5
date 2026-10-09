# 15. Configuration internals

**Abstract.** [`README.md`](../README.md) explains what each setting in `appsettings.json` means.
This chapter explains how the code reads the settings. It describes the helper methods that give
access to them, the value that is used when a setting is missing or cannot be read, and the moment
at which each setting is read. It compares the defaults in the code with those in the shipped
file, and shows which changes take effect while the node runs. It ends with the fixed values that
cannot be configured and with the steps for adding a new setting.

## 15.1 Overview

The node uses the standard configuration system of .NET. Chapter 4 lists the configuration sources
and their priority (Listing 4.1). The most important rule is repeated here: a file given with
`--config` has the highest priority and overrides even command-line arguments.

The code does not use options classes. It reads values directly from `IConfiguration`, but never by
name at the place of use. Every setting has a helper method that knows its name, its type and its
default value.

## 15.2 Helper methods

The helper methods are extension methods of `IConfiguration`, in two files:

- `Enigma5.App.Common/Extensions/ConfigurationExtensions.cs` holds one method per setting, for
  example `GetHostname()` or `GetMessageRetentionPeriod()`. It is in the `Common` project because
  `Enigma5.Security` reads settings too.
- `Enigma5.App/Extensions/ConfigurationExtensions.cs` holds the methods for the two blacklist
  settings, the blacklist checks (Chapter 14), and the warnings about settings that cannot be read
  (Section 15.3).

Every setting with a type other than text is defined once, in the Common file, as a private
`TypedSetting<T>`: its name, its default from `Constants.cs`, and the function that reads its
value. The getter of the setting and the check for values that cannot be read both use this
definition, so they cannot disagree. All such definitions are listed in the array
`TypedSettings`.

Two settings are not read by these helpers but by the libraries they belong to: Kestrel reads the
section `Kestrel`, and Serilog reads the section `Serilog` (Chapter 18).

## 15.3 How values are read

Table 15.1 shows how each type of value is read. For every type with rules, a value that cannot be
read is handled the same way: the default from `Constants.cs` is used, and a warning names the
setting, the value found and the default used.

**Table 15.1:** Reading rules by type of value.

| Type | Settings | Rule |
|---|---|---|
| Text | Paths, connection string, `Hostname`, `OnionService`, `Socks5Proxy`, `AzureVaultUrl` | An empty value, or one that holds only spaces, counts as missing. Any other value is used as it is. |
| URL | `Hostname`, the two Kestrel URLs | As text; slashes and spaces at both ends are removed. |
| Time span | The four retention periods, `VertexLifetime`, `UnlistedVertexGracePeriod` | Format `d.hh:mm:ss`, for example `14.00:00:00`, read without regard to the system's language settings. |
| Choice | `KeySource`, `PassphraseSource`, `PassphrasePersistence`, `DbProvider` | One of the names of the matching `enum`; case does not matter. A number is accepted only if it stands for a defined choice. |
| Number | `SharedDataMaxSize`<br>`SharedFileMaxSize`<br>`Network:`<br>`DelayBetweenConnectionRetries` | A whole number. |
| List | `HttpBlacklists`, `HubBlacklists` | Bound to data objects (Chapter 14). |

A missing value is replaced by the default in the code without a warning (Section 15.4). For the
lists the default is an empty list.

The node checks all typed settings at startup and writes one warning for each value it cannot read,
for example:

```text
The setting PassphrasePersistence has the value Foo, which cannot be read.
The default Persistent is used instead.
```

The check runs again every time a configuration file is loaded again, so a mistake made while the
node runs is reported too. A single save of a file can cause two reloads, and then two identical
warnings.

The check does not judge values that can be read. A negative period or a very short
`VertexLifetime` is used as it is (Section 15.4).

## 15.4 Defaults in the code and in the file

Each helper has a default that is used when the setting is missing. Table 15.2 lists all settings
with this default and with the value in the shipped `appsettings.json`.

**Table 15.2:** Settings, their defaults, and when they are read.

| Setting | Default in the code | Value in the shipped file | Read |
|---|---|---|---|
| `ConnectionStrings:`<br>`DbConnectionString` | `data source=`<br>`aenigmaDb.sqlite` | `data source=`<br>`aenigmaDb.sqlite` | When a database context is created |
| `DbProvider` | `Sqlite` | `Sqlite` | At startup |
| `Kestrel:EndPoints:Http:Url` | Not set | `http://127.0.0.1:8080` | At startup; when the bridge connects |
| `Kestrel:EndPoints:`<br>`HttpControl:Url` | Not set | `http://127.0.0.1:8081` | At startup; when the bridge connects |
| `HttpBlacklists`,<br>`HubBlacklists` | Empty | Two rules (Chapter 14) | At every request or hub call |
| `Hostname`, `OnionService` | Not set | Not set | At each use |
| `Socks5Proxy` | `socks5://127.0.0.1:9050` | `socks5://127.0.0.1:9050` | When the bridge connects to an onion address |
| `KeySource` | `File` | `File` | At startup |
| `PassphraseSource` | `Dashboard` | `Dashboard` | At startup |
| `PassphrasePersistence` | `Persistent` | `Persistent` | At each key setup |
| `PrivateKeyPath`,<br>`PublicKeyPath` | `private-key.pem`,<br>`public-key.pem` | `private-key.pem`,<br>`public-key.pem` | When a key is read |
| `AzureVaultUrl`,<br>`PassphrasePath` | Not set | Not set | Used by the Azure key source only (Chapter 6) |
| `WebContentDirectory` | `./` | `./` | At each use |
| `MessageRetentionPeriod` | 14 days | 14 days | At startup |
| `SentMessageRetentionPeriod` | 0 | 0 | At startup |
| `SharedDataRetentionPeriod` | 14 days | 14 days | At startup, and for each `validUntil` value |
| `FilesRetentionPeriod` | 3 days | 3 days | At startup, and for each `validUntil` value |
| `SharedDataMaxSize` | 16 KiB | 16 KiB | At startup |
| `SharedFileMaxSize` | 64 MiB | 64 MiB | At startup |
| `VertexLifetime` | 30 minutes | 30 minutes | At each check of the graph |
| `UnlistedVertexGracePeriod` | 6 minutes | 6 minutes | At each cleanup of the graph |
| `Network:`<br>`DelayBetweenConnectionRetries` | 3000 (milliseconds) | 3000 | When a connection to a peer closes |

All defaults are defined in `Constants.cs` and are the same as the values in the shipped file.
Four settings have no default on purpose: for `Hostname`, `OnionService`, `AzureVaultUrl` and
`PassphrasePath`, "not set" is a meaningful value. The two Kestrel URLs have no default in the code
either: Kestrel reads them itself and has its own fallback, which a default in the code could
contradict. A setting that is missing, or a time span that cannot
be read, therefore behaves like the shipped configuration. This matters most for the retention
periods: a period of 0 makes the cleanup jobs delete every record at their next run (Chapters 9
and 17), so 0 must never be the result of a mistake. The defaults are defined in `Constants.cs`
(`DefaultMessageRetentionPeriod` and the like).

Two settings of the network graph (Chapter 10) depend on each other and on fixed values of the
protocol:

- `VertexLifetime` must be well above ten minutes. A connected node sends its vertex every five
  minutes, and an unchanged vertex is accepted again only after six minutes, so a live neighbor's
  vertex is refreshed only about every ten minutes. A shorter lifetime would make the cleanup remove
  live neighbors between refreshes.
- `UnlistedVertexGracePeriod` should be shorter than `VertexLifetime`; otherwise unlisted vertices
  are only ever removed by expiry, and the setting has no effect. It should not be shorter than
  about a minute, so that a vertex is not removed before the vertex that lists it has arrived.

Neither condition is checked at startup.

## 15.5 Changes while the node runs

The JSON configuration files are watched and loaded again when they change. Whether a change has
an effect depends on when the code reads the setting (the last column of Table 15.2).

- Settings read **at each use** take effect at once. This holds for the blacklists
  (Chapter 14) and for `Hostname` as returned by `GET /Info`.
- Settings read **at startup** need a restart. This covers the choice of key source and passphrase
  source, the database, the two size limits and the retention periods. The retention periods are
  read when the cleanup jobs are registered, and the values are stored with the jobs (Chapter 17).

A value can be used in more than one place, and not all places see a change at the same time. The
hostname is one example. `GET /Info` reads it at each request. The node's own vertex, however, was
signed with the old hostname and keeps it until the vertex is signed again (Chapter 10). After a
change of `Hostname` or `OnionService`, a restart is the simplest way to bring all places into
agreement.

## 15.6 Derived values

Some helpers do not return a setting but a value built from settings.

**Table 15.3:** Helpers that return derived values.

| Helper | Result |
|---|---|
| `GetPublicEndpoint()` | `Hostname` if it is set, otherwise `OnionService`, otherwise nothing. |
| `GetSharedDataUrl(tag)`<br>`GetFileUrl(tag)` | The public endpoint followed by `/Share?Tag=` or `/File?Tag=` and the tag. Nothing if there is no public endpoint. |
| `GetSharedDataValidityDate()`<br>`GetFileValidityDate()` | The current time plus the matching retention period. |
| `GetWebContentFilePath(tag)` | The full path of an uploaded file. Nothing if the tag is not a GUID or the path would lie outside `WebContentDirectory` (Chapter 12). |

## 15.7 Fixed values

Some values are constants in `Enigma5.App.Common/Constants.cs` and cannot be changed by
configuration. Table 15.4 lists the most important ones.

**Table 15.4:** Fixed values.

| Constant | Value | Meaning |
|---|---|---|
| `MessagesPageSize` | 20 | Messages per `Pull2` page; also the largest number of payloads in one `RouteMessage` call (Chapter 9). |
| `LegacyPullMaxMessages` | 128 | Largest number of messages returned by `Pull`. |
| `MaxOnionSize` | 16,384 | Largest length of one onion, in base64 characters (Chapter 5). |
| `SignalRMaximum`<br>`ReceiveMessageSize` | 331,776 | Largest hub message, in bytes: 20 onions of the largest size, plus 4 KiB. |
| `MinPublicKeySize`<br>`MaxPublicKeySize` | 2,048<br>8,192 | Accepted sizes of a caller's RSA key, in bits (Chapter 5). |
| `MaxPublicKeyLength` | 4,096 | Longest text of a public key, in characters. |
| `AuthTokenSize` | 64 | Size of the sign-in nonce, in bytes (Chapter 8). |
| `DefaultVertexBroadcast`<br>`MinimumPeriod` | 6 minutes | Shortest time before an unchanged vertex is accepted again (Chapter 10). |
| `SignalRKeepAliveInterval`<br>`SignalRClientTimeoutInterval`<br>`SignalRHandshakeTimeout` | 20 s<br>90 s<br>15 s | Timing of hub connections. |
| `SignalRReconnectDelays` | 2, 4, 8, 16 s | Waiting times before the bridge tries to connect again (Chapter 11). |
| `...JobInterval` | Every 5 minutes | Schedule of the background jobs (Chapter 17). |

The constants also hold the paths of the HTTP endpoints and of the hub, the names of the
background jobs, and the names of the log properties (Chapter 18).

## 15.8 Adding a setting

1. Add a helper method to `Enigma5.App.Common/Extensions/ConfigurationExtensions.cs`. For a text
   setting, use `GetStringValue`. For any other type, define a `TypedSetting<T>` with the matching
   parser, add it to the array `TypedSettings`, and let the helper call its `Read` method. Then the
   setting follows the rules of Table 15.1 and is checked at startup.
2. Choose the default in the code so that a missing setting is harmless, and keep it equal to the
   value in the shipped file. Define it in `Constants.cs`.
3. Add the setting with its normal value to `Enigma5.App/appsettings.json` and to the
   configuration files of the packages (Appendix A).
4. Describe the setting in [`README.md`](../README.md).
5. Read the setting through the helper at the place of use. Decide whether it should be read at
   each use, so that changes apply at once, or once at startup.

## 15.9 Summary

Settings are read from `IConfiguration` through one helper method per setting. A file given with
`--config` overrides all other sources. Missing values, and most values that cannot be read, are
replaced by a default from `Constants.cs`, which is the same as the value in the shipped file. A
missing setting therefore does no harm, and a value that cannot be read is reported with a warning,
at startup and whenever a configuration file is loaded again. The
configuration files are loaded again when they change. Settings that are read at each use, such as
the blacklists, then take effect at once; settings read at startup need a restart. Limits of the
protocol, such as the page size and the largest onion size, are constants and cannot be configured.
