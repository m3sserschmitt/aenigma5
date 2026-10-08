# 7. The SignalR hub pipeline

**Abstract.** Clients and peer nodes talk to a node mainly through one SignalR hub. This chapter
describes how the hub is built: its class, its connection settings, and the chain of filters every
hub call passes through. It explains how a filter decides whether to run, how filters hand data to
the hub method, and how results and errors are returned. It then covers input validation, calls that
carry several onions at once, and calls the server makes to clients. The chapter ends with a
checklist for adding a hub method. [`SIGNALR_API.md`](../SIGNALR_API.md) describes each hub method
from the caller's side; this chapter describes the machinery behind them.

## 7.1 The hub

The hub is the class `RoutingHub`, mapped to the path `/OnionRouting`. The class is split over two
files: `Enigma5.App/Hubs/RoutingHub.cs` contains the public hub methods and the connection events,
and `RoutingHub2.cs` contains private helpers.

SignalR creates a new `RoutingHub` object for every hub method call. Besides the SignalR `Hub` base
class, `RoutingHub` implements four interfaces:

- `IEnigmaHub`, the list of hub methods;
- `IIdentityHub`, with the property `ClientAddress`: the address of the signed-in caller;
- `IOnionParsingHub`, with the properties `Next`, `Content` and `Uuid`: the result of removing one
  onion layer;
- `IOnionRoutingHub`, with the property `DestinationConnectionId`: the connection of the next stop,
  if it is connected.

Filters fill in these properties before the hub method runs (Section 7.3.4). Table 7.1 lists the hub
methods and the filters turned on for each of them. The columns stand for the marker attributes
`[Authenticated]` (sign-in), `[BlacklistAuthorization]` (blacklist), `[ValidateModel]` (validation),
and `[OnionParsing]` together with `[OnionRouting]` (onion).

**Table 7.1:** Hub methods and the filters turned on for each.

| Method | Sign-in | Blacklist | Validation | Onion |
|---|---|---|---|---|
| `GenerateToken` | | Yes | | |
| `Authenticate` | | Yes | Yes | |
| `GetLocalVertex` | | Yes | | |
| `Pull` (deprecated) | Yes | Yes | | |
| `Pull2` | Yes | Yes | Yes | |
| `Cleanup` (deprecated) | Yes | Yes | | |
| `Cleanup2` | Yes | Yes | Yes | |
| `RouteMessage` | Yes | Yes | Yes | Yes |
| `Broadcast` | Yes | Yes | Yes | |
| `TriggerBroadcast` | Yes | Yes | Yes | |

The hub also handles two connection events. `OnConnectedAsync` stores three details of the new
connection in `Context.Items`: the value of the `X-Impersonate-Service` header (Chapter 8), and the
local IP address and port the connection arrived on (Chapter 14). `OnDisconnectedAsync` ends the
connection's session (Chapter 8).

## 7.2 Connection settings

Table 7.2 lists the SignalR settings of the hub. The values set by the node are defined in
`Enigma5.App.Common/Constants.cs`.

**Table 7.2:** Hub connection settings.

| Setting | Value | Set by |
|---|---|---|
| Keep-alive interval | 20 s | Node |
| Client timeout | 90 s without a message from the client | Node |
| Handshake timeout | 15 s | Node |
| Stateful reconnect | On; up to 1 MiB of messages kept for a reconnecting client | Node |
| Maximum size of one incoming message | 331 776 bytes (324 KiB) | Node |
| Transports | WebSockets, Server-Sent Events, long polling | Framework default (not changed) |

The maximum message size applies to a whole hub call, including all its arguments. A larger message
is dropped by SignalR before any node code runs, and the caller gets no reply. The value is
computed from two constants, so that a full batch always fits: 20 onions
(`Constants.MessagesPageSize`) of the maximum size of 16 384 characters (`Constants.MaxOnionSize`),
plus 4 KiB for the JSON around them. A single onion that is too long is rejected by validation, with an error, before
it is processed (Table 7.4). Chapter 5 explains how the maximum onion size was chosen.

## 7.3 Filters

### 7.3.1 Order

Every hub call passes through six filters, registered in `StartupConfiguration.ConfigureServices`.
SignalR runs them in the order they were registered: the first one wraps all the others, and the last
one calls the hub method.

**Listing 7.1:** Order of the hub filters.

```text
call ─► LogFilter
          └► AuthenticatedFilter
               └► BlacklistAuthorizationFilter
                    └► ValidateModelFilter
                         └► OnionParsingFilter
                              └► OnionRoutingFilter
                                   └► hub method
```

The order has visible effects. A caller that is not signed in gets `Authentication required` even
for a method that is blocked on its endpoint, because the sign-in check runs first. And input is
validated only after the sign-in check (where it applies) and the blacklist check have passed.

### 7.3.2 When a filter runs

All filters except `LogFilter` derive from `BaseFilter<THub, TMarker>`. Such a filter does its work
only if three conditions hold:

1. the hub method has the marker attribute `TMarker`;
2. the hub implements the interface `THub`;
3. the arguments of the call pass the filter's own check (for example, "exactly one argument, and it
   can be validated").

If any condition is false, the filter is skipped and the call goes on to the next filter. A filter is
therefore not a hard barrier: a call whose arguments do not fit a filter's check simply passes by
that filter. The later filters and the hub method must still handle such calls safely. For example,
a `RouteMessage` call with a `null` argument skips validation and onion parsing; `OnionRoutingFilter`
then finds no next address and returns an error.

### 7.3.3 What each filter does

**Table 7.3:** The hub filters.

| Filter | Marker | What it does | Result when it rejects a call |
|---|---|---|---|
| `LogFilter` | none (always runs) | Logs every call with its arguments and its result. Catches exceptions and `null` results. | `Internal error` |
| `AuthenticatedFilter` | `[Authenticated]` | Looks up the address signed in on this connection (Chapter 8) and stores it in `ClientAddress`. | `Authentication required` |
| `BlacklistAuthorizationFilter` | `[BlacklistAuthorization]` | Checks the method against `HubBlacklists` for the endpoint the connection arrived on (Chapter 14). | `Internal error` |
| `ValidateModelFilter` | `[ValidateModel]` | Calls `Validate()` on the single argument (Section 7.5). | The list of validation errors |
| `OnionParsingFilter` | `[OnionParsing]` | Removes one layer from each payload and calls the rest of the chain once per payload (Section 7.6). | `Could not parse onion.` for each payload that fails |
| `OnionRoutingFilter` | `[OnionRouting]` | Looks up whether the next address is connected and, if so, stores its connection in `DestinationConnectionId`. | `Failed to route or store message.` if there is no next address |

### 7.3.4 Passing data to the hub method

A filter cannot change the arguments of a call, so it passes data to the hub method through the hub
object instead. The *adapter* classes in `Hubs/Adapters` do this. Each adapter takes the hub object,
checks that it implements the matching interface, and sets properties on it. For example,
`AuthenticatedFilter` runs `new IdentityHubAdapter(hub) { ClientAddress = address }`. Since SignalR
creates a new hub object for every call, these values belong to one call only.

## 7.4 Results and errors

Every hub method returns an `InvocationResultDto<T>`. Listing 7.2 shows its JSON form.

**Listing 7.2:** JSON form of a hub result.

```text
{
  "data":    <result value, or null>,
  "success": true | false,
  "errors":  [ { "message": "<text>", "properties": ["<name>", ...] | null }, ... ]
}
```

`SuccessResultDto<T>` sets `success` to `true` with no errors. `ErrorResultDto<T>` sets it to
`false`. The error texts are defined in `Hubs/InvocationErrors.cs` and
`Enigma5.App.Models/ValidationErrorsDto.cs`; [`SIGNALR_API.md`](../SIGNALR_API.md) lists which method
can return which text.

Two errors are equal if their messages are equal. When errors are collected, an error with a message
that is already in the list is not added again; instead, its property names are merged into the
existing entry. So "One or more required properties not provided." appears once, with the names of
all missing properties.

## 7.5 Input validation

Request objects that can be validated implement `IValidatable`. Their `Validate()` method returns the
list of errors. Table 7.4 lists the rules.

**Table 7.4:** Validation rules of hub request objects.

| Request object | Used by | Rules |
|---|---|---|
| `AuthenticationRequestDto` | `Authenticate` | `publicKey` is present and is a PEM public key; `signature` is present and is base64. |
| `PullRequestDto` | `Pull2` | `infId` is `null` or not negative. |
| `CleanupRequestDto` | `Cleanup2` | `supId` is present and not negative. |
| `RoutingRequestDto` | `RouteMessage` | `payloads` has 1 to 20 items; every item is at most 16 384 characters long and is base64. `uuid`, if given, is a GUID and comes with exactly one payload. |
| `VertexBroadcastRequestDto` | `Broadcast` | `publicKey` is present and is a PEM public key; `signedData` could be split into data and signature, and the data is a valid neighborhood (address present and valid, `neighbors` present and all valid). |
| `TriggerBroadcastRequestDto` | `TriggerBroadcast` | Every item of `newAddresses` is a valid address. |

`VertexBroadcastRequestDto` does part of its work while it is being read from JSON: setting
`signedData` splits it into data and signature and reads the neighborhood from the data. Validation
only checks the result. The signature itself is checked later, by the network graph (Chapter 10).

## 7.6 Calls with several onions

A `RouteMessage` call can carry up to 20 payloads. Calls with several payloads are meant for
clients only. Before it processes any payload, `OnionParsingFilter` checks whether the caller's
address is one of the node's neighbors (Chapter 10), that is, another relay. A relay must send
exactly one payload per call, together with its `uuid`; otherwise the call is rejected with `Relays
must route one message per request, with its uuid.`.

The filter then handles the payloads one by one:

1. It removes one layer from the payload (Chapter 5).
2. If that fails, it records `Could not parse onion.` and goes on with the next payload.
3. If it works, it sets `Next`, `Content` and `Uuid` on the hub object and calls the rest of the
   chain. `OnionRoutingFilter` and the hub method `RouteMessage` therefore run once *per payload*.
4. It collects the errors of each run and keeps the first successful result.

After the last payload, the filter returns an error result if any payload produced an error, and
the first successful result otherwise. This has an important effect: **a call can report failure
even though some of its payloads were stored.** A caller that sends several payloads cannot tell from the result which ones were
stored. This is intended: invalid onions are reported and dropped, the valid ones are processed,
and following the format rules is the responsibility of the sender. A `uuid` is only allowed in a
call with exactly one payload; validation rejects a call with several payloads and a `uuid`
(Chapter 9).

## 7.7 Calls from the server to clients

The hub also calls methods on connected clients: `RouteMessage` to deliver a message at once
(Chapter 9), and `Broadcast` to send graph updates to neighbor nodes (Chapter 10).
[`SIGNALR_API.md`](../SIGNALR_API.md) describes both from the client's side.

These calls are made by `RoutingHub.SendAsync`. It starts the call on the thread pool and does not
wait for it, so the result is always reported as successful, even if the client never gets the
message. The name of the method called for message delivery is found by reflection: it is the name
of the hub method that has the `[OnionRouting]` attribute.

## 7.8 Adding a hub method

A new hub method should be added in these steps:

1. Add the method to `IEnigmaHub` and implement it in `RoutingHub.cs`, returning an
   `InvocationResultDto<T>`.
2. Add `[BlacklistAuthorization]`, so that operators can block it per endpoint.
3. Add `[Authenticated]` if only signed-in callers may use it. `ClientAddress` is then set.
4. If it takes an argument, make the argument class implement `IValidatable` and add
   `[ValidateModel]`. Use a single argument; filters only check calls with one argument.
5. Keep the hub method thin: send a MediatR command or query and turn its `CommandResult` into a hub
   result (Chapter 2).
6. Return errors with the texts in `InvocationErrors`, and document the method in
   [`SIGNALR_API.md`](../SIGNALR_API.md).

## 7.9 Summary

The hub `RoutingHub` at `/OnionRouting` is created anew for every call. Every call passes six filters
in a fixed order: logging, sign-in check, blacklist check, validation, onion parsing and onion
routing. A filter runs only if the hub method has its marker attribute and the call's arguments fit
the filter, and otherwise it is skipped. Filters pass their results to the hub method through
properties of the hub object. Every method returns a result object with data, a success flag and a
list of errors. A hub message may be at most 324 KiB, and one onion at most 16 384 base64
characters. A call with several onions runs the rest of the
chain once per onion, and reports failure if any onion fails, even when others were stored. Calls
from the server to clients are not waited for.
