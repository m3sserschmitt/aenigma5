# 14. Access control

**Abstract.** A node has no user accounts and no roles. Access is controlled in three ways: by the
network endpoint a request arrives on, by blacklists that block HTTP paths and hub methods per
endpoint, and by the sign-in that some hub methods require. This chapter describes the blacklists
in detail: their structure, how a request is matched against them, and what the caller receives
when a request is blocked. It also states what the blacklists do not cover. [`README.md`](../README.md) shows the default blacklist
settings.

## 14.1 Overview

Table 14.1 lists the three means of access control and where each one is described.

**Table 14.1:** Means of access control.

| Means | Decides by | Applies to | Described in |
|---|---|---|---|
| Endpoint | Where Kestrel listens, and who can reach that address | Everything | Chapter 4 |
| Blacklists | The endpoint a request arrived on, and the path or hub method it asks for | HTTP requests and hub method calls | This chapter |
| Sign-in | Proof that the caller holds the private key of an address | Hub methods marked `[Authenticated]` | Chapter 8 |

The idea behind the first two is simple. A node listens on two endpoints, a public one and a
control one (Chapter 4). Both serve the same application. The operator makes only the public
endpoint reachable from outside, and the blacklists remove from it what outside callers must not
use. The control endpoint stays unrestricted and must be reachable only by the operator and by the
node itself.

## 14.2 Structure of a blacklist

There are two settings, `HttpBlacklists` and `HubBlacklists`. Each is a list of blacklists, and
each blacklist belongs to one endpoint.

**Listing 14.1:** Structure of the two blacklist settings.

```text
HttpBlacklists = [ blacklist, ... ]
    blacklist  = { Endpoint, Items: [ item, ... ] }
    item       = { Path, Methods: [ HTTP method, ... ] }

HubBlacklists  = [ blacklist, ... ]
    blacklist  = { Endpoint, Items: [ item, ... ] }
    item       = { Methods: [ hub method name, ... ] }
```

Everything that is not listed is allowed. An empty or missing setting allows everything on every
endpoint. A blacklist without `Items` is left out when the setting is read; it blocks nothing, and
the warning of Section 14.3 is not written for it.

The settings are read as the data objects `HttpBlacklistDto`, `HttpBlacklistItemDto`,
`HubBlacklistDto` and `HubBlacklistItemDto` from `Enigma5.App.Models`. The checks themselves are in
`Enigma5.App/Extensions/ConfigurationExtensions.cs`.

## 14.3 Matching the endpoint

A blacklist applies to a request if its `Endpoint` matches the local address and port on which the
request arrived. The address of the caller plays no part. The comparison is done by `MatchUrl`
(`Enigma5.App.Common/Extensions/StringExtensions.cs`).

**Listing 14.2:** Rules of `MatchUrl(endpoint, localIp, localPort)`.

```text
1. endpoint must be an absolute URL, otherwise: no match
2. the host of endpoint must be an IP address, otherwise: no match
3. if the host is 0.0.0.0: match if the ports are equal
4. otherwise: match if the IP addresses and the ports are equal
   (an IPv4 address written in IPv6 form is treated as the IPv4 address)
```

Three consequences are important for operators:

- The host must be written as an IP address. A blacklist for `http://localhost:8080` never
  matches, and its rules have no effect. At startup, the node logs a warning for every blacklist
  whose `Endpoint` can never match.
- The scheme (`http` or `https`) and any path in `Endpoint` are not compared.
- Only the first blacklist that matches the endpoint is used. If two blacklists name the same
  endpoint, the second one has no effect. All rules for one endpoint must be put into one
  blacklist.

When the value of `Kestrel:EndPoints:Http:Url` is changed, the `Endpoint` values of the blacklists
must be changed with it. Otherwise the blacklists no longer match and nothing is blocked.

## 14.4 HTTP requests

`HttpBlacklistAuthorizationMiddleware` checks every request that reaches it (Chapter 4). A request
is blocked if an item of the matching blacklist fulfils both conditions:

- the request path begins with the item's `Path`, compared segment by segment and without regard
  to upper and lower case;
- the request method is one of the item's `Methods`, also without regard to case.

A blocked request is answered with status 404 and an empty body. To the caller, a blocked path
looks like a path that does not exist.

If the blacklists are not empty and the local address of the request cannot be found, the request
is blocked and an error is logged.

**Table 14.2:** How the default blacklist applies to requests on the public endpoint.

| Request | Blocked | Reason |
|---|---|---|
| `GET /Dashboard` | Yes | Listed. |
| `GET /dashboard` | Yes | Case does not matter. |
| `GET /Dashboard/x` | Yes | The path begins with the segment `/Dashboard`. |
| `HEAD /Dashboard`, `POST /Dashboard` | No | Only `GET` is listed. The page itself rejects these requests. |
| `POST /_blazor/negotiate` | Yes | Second default rule (Section 14.6). |
| `GET /app.css` | No | Static file; never checked (Section 14.7). |

On the control endpoint none of these requests is blocked.

## 14.5 Hub method calls

`BlacklistAuthorizationFilter` is the third filter of the hub pipeline (Chapter 7). It runs for hub
methods marked with `[BlacklistAuthorization]`. All methods of `RoutingHub` carry this marker.

The filter needs the local address and port of the connection. It takes them from the HTTP context
of the connection. If no HTTP context is available, it uses the values that `OnConnectedAsync`
stored in the connection's items when the connection was opened (`MapConnectionDetails`).

A call is blocked if the name of the hub method is listed in an item of the matching blacklist.
Names are compared without regard to case. A blocked call returns the error `Internal error`
(Chapter 7). The text does not tell the caller that the method exists but is blocked.

Only method calls are checked. A client can always open a connection to the hub on any endpoint.

## 14.6 Default rules

The default configuration contains three rules, all for the public endpoint.

**Table 14.3:** Default blacklist rules.

| Setting | Rule | Purpose |
|---|---|---|
| `HttpBlacklists` | `GET /Dashboard` | The dashboard can unlock and lock the key and change the peers (Chapter 16). It must be reachable by the operator only. |
| `HttpBlacklists` | `GET` and `POST` on `/_blazor` | The dashboard page talks to the node through this SignalR endpoint of Blazor (Chapter 16). Blocking it closes the dashboard's own connection as well. |
| `HubBlacklists` | `TriggerBroadcast` | The method makes the node send its vertex to a peer. Only the node's own network bridge calls it, through the control endpoint (Chapter 11). |

The development configuration (`Enigma5.App/appsettings.json`) has one more rule: `GET` and `POST`
on `/Jobs`. This path is the jobs page of Hangfire, which exists only in the `Development`
environment (Chapter 17).

## 14.7 What the blacklists do not cover

- **Static files.** The static file middleware runs before the blacklist middleware (Chapter 4).
  Files in `wwwroot`, such as the dashboard's style sheets, are served on every endpoint.
- **Other HTTP methods.** A rule blocks only the methods it lists (Table 14.2).
- **The caller.** A blacklist cannot tell callers apart. Whoever can reach the control endpoint
  can use the dashboard, which has no sign-in of its own. The node must therefore be set up so
  that only the operator can reach that endpoint, for example by binding it to the loopback
  address (the default) and by not publishing it. Other programs on the same machine can reach
  the loopback address too.
- **Connections through a proxy.** When a reverse proxy or a Tor onion service passes requests to
  the node, the decision still depends only on the local endpoint the proxy connects to. Each
  proxy route must therefore point to the endpoint with the intended rules.

## 14.8 When changes take effect

The blacklists are read from the configuration at every HTTP request and at every hub method call.
A changed configuration file is loaded again while the node runs (Chapter 15), so a new rule
applies from the next request on, without a restart.

## 14.9 Rules for contributors

- A new hub method must carry `[BlacklistAuthorization]`. Without the marker, operators cannot
  block the method (Chapter 7).
- A new HTTP endpoint is checked by the middleware without further work. If only the operator or
  the node itself may use it, a rule for the public endpoint must be added to the default
  `appsettings.json` and to the configuration files of the packages (Appendix A).
- Content that only the operator may see must not be placed in `wwwroot`.

## 14.10 Summary

A node has no accounts. It separates outside callers from the operator by endpoint: both endpoints
serve the same application, and blacklists remove paths and hub methods from the public one. A
blacklist belongs to an endpoint given as an IP address and port, and only the first blacklist for
an endpoint is used. A blacklist that can never match is reported at startup. Blocked HTTP requests are answered with 404, blocked hub calls with
`Internal error`. The rules are read at every request, so changes apply without a restart. The
blacklists do not cover static files or methods that are not listed, and they cannot tell callers
apart. The control endpoint must therefore be reachable by
the operator only.
