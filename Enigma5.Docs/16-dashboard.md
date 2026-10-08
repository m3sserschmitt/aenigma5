# 16. Dashboard

**Abstract.** The dashboard is the web page through which an operator manages a running node. On
it, the operator unlocks and locks the node's private key, sees the data that clients need to use
the node, and adds or removes peers. This chapter describes how the page is built with Blazor
Server, how it receives changes from the rest of the node through `DashboardUIState`, and which
commands each action sends. It also describes what the page shows in each state, and the limits of
the current design.

## 16.1 Overview

The dashboard is one page, `/Dashboard`. In the default configuration it is served on the control
endpoint only, at `http://127.0.0.1:8081/Dashboard` (Chapter 14). It has no sign-in of its own:
whoever can reach the control endpoint can use it.

The page has three parts:

- the **key card**, with a passphrase field and a button that unlocks or locks the private key;
- the **contact card**, with a QR code, the node's address and its onion service;
- the **peers card**, with the list of peers and a form for adding one.

The dashboard contains no logic of its own. Every action sends a MediatR command, as the hub and
the HTTP API do (Chapter 2). The page only shows the result.

## 16.2 Blazor Server

The dashboard is built with Razor components and *interactive server rendering*, also called
Blazor Server. In this model the components run on the node, not in the browser:

1. The browser requests `/Dashboard`. The node renders the page and returns it as HTML.
2. A script in the page (`_framework/blazor.web.js`) opens a SignalR connection to the node at
   `/_blazor`. This connection is separate from the routing hub (Chapter 7).
3. Clicks and input are sent to the node over this connection. The component code runs on the
   node, and the node sends back the parts of the page that changed.

Two things follow from this. First, the component code can use the node's services directly, for
example `IMediator`. No extra API is needed for the dashboard. Second, the node can change the page
without a request from the browser: when the state of the node changes, an open dashboard shows it
at once.

The script must be available for step 2. Without it the page is shown but does not react to
clicks. Section 16.8.1 describes where the script comes from.

## 16.3 Structure

The code of the dashboard is in `Enigma5.App/UI/`.

**Table 16.1:** Files of the dashboard.

| File | Content |
|---|---|
| `App.razor` | The HTML document: style sheets, the `Routes` component and the Blazor script. |
| `Routes.razor` | The router, which maps the path `/Dashboard` to the page. |
| `Layout/MainLayout.razor` | The frame around the page content. |
| `Pages/Dashboard.razor` | The page. Arranges the three cards and sends all commands. |
| `Dashboard/PassphraseInput.razor` | The key card. |
| `Dashboard/`<br>`ExportContactDataQrCode.razor` | The contact card. |
| `Dashboard/PeersInput.razor` | The peers card: list, *Retry* button and form. |
| `Dashboard/PeerItem.razor` | One row of the peers list. |
| `Common/ItemsList.razor` | A general list component. |
| `Common/ToastHost.razor` | Short messages in the top right corner (Section 16.7.4). |
| `DashboardUIState.cs` | The data shown on the dashboard, shared by all open pages (Section 16.4). |

Style sheets and icons (Bootstrap, Bootstrap Icons, `app.css`) are static files in
`Enigma5.App/wwwroot/`.

The cards do not send commands themselves. They report an action to the page through a
parameter, for example `OnUnlockKey` or `OnAddItem`, and the page sends the command. Most of these
parameters are of type `EventCallback`. `OnAddItem` is a function that returns whether the peer was
added, because the card clears its two input fields only in that case.

## 16.4 DashboardUIState

The dashboard shows data that changes while the page is open: whether the key is unlocked, and
which peers exist. Other parts of the node own this data. `DashboardUIState` is the link between
them and the page. It is registered once for the whole node (singleton), so that every open
dashboard page sees the same values.

**Table 16.2:** Values of `DashboardUIState`.

| Value | Meaning | Set by |
|---|---|---|
| `PrivateKeyUnlocked` | `true` if the node's own vertex is signed (Chapter 10). | `SetMasterPassphraseHandler`<br>`RemoveMasterPassphraseHandler` |
| `OutboundPeers` | The peers stored in the `Peers` table (Chapter 11). | `InvokeNetworkBridgeHandler` |
| `InboundPeers` | The nodes that are connected with this node: their vertex lists this node as a neighbor, and this node's own vertex lists them (Chapter 10). | `NetworkGraph` |

Each value has a read property, a `Set...Async` method and a change event.

`DashboardUIState` owns a single-thread runner (Chapter 3). A setter is one work item, and the
notification of the pages is a second one, as shown in Listing 16.1.

**Listing 16.1:** Setting a value of `DashboardUIState`.

```text
work item 1 (the caller waits for it):
    if the new value equals the old one: stop
    store the new value
    queue work item 2
work item 2 (nobody waits for it):
    for every handler of the change event:
        start the handler with its own copy of the value; do not wait for it
        if the handler fails, log the error
```

This design has four properties.

- **Order.** Notifications are queued in the order in which the values were stored, so a page
  never ends up showing an older value than the stored one.
- **Short waits.** The caller of a setter waits only until the value is stored. This matters for
  `NetworkGraph`, which calls `SetInboundPeersAsync` from its own runner thread: the graph is not
  held up by the pages.
- **Isolation.** A handler is started and not waited for. A page that is slow, or whose handler
  fails, delays neither the runner nor the other pages. The failure is written to the log.
- **Copies.** Every reader and every handler receives its own `PeerDto` objects. A page may change
  them, as `Dashboard.razor` does when it marks peers as connected, without any effect on the
  stored values or on other pages.

The read properties are synchronous and do not use the runner. A stored value is written only by
the runner and is replaced as a whole, so it can be read at any time. This keeps the start of a
page simple and safe: a component subscribes to the events in `OnInitializedAsync` and reads the
current values in the same step, so no change can slip in between. It unsubscribes in `Dispose`,
when the page is closed.

An event handler is called on the runner thread of `DashboardUIState`. It therefore passes its
work to the page's own context with `InvokeAsync` before it touches the page. There it stores the
new value and calls `StateHasChanged`. The call is needed because the notification is a work item
of its own (Listing 16.1): it can reach the page after the click that caused the change has
already been drawn. Without the call, a node without peers showed *Unlock Private Key* after a
correct unlock until the page was loaded again.

## 16.5 The key card

**Table 16.3:** Actions on the key card.

| Action | Command | Effect |
|---|---|---|
| *Unlock Private Key* | `SetMasterPassphraseCommand` with the passphrase | Runs the key setup of Chapter 4: stores the passphrase in the kernel keyring, signs the node's vertex, and starts the network bridge. |
| *Lock Private Key* | `RemoveMasterPassphraseCommand` | Removes the passphrase from the keyring and builds the node's vertex again, now without a signature (Chapter 6). Then starts the bridge, which closes the connections to the peers because the key can no longer sign (Chapter 11). |

The card shows one of the two buttons, depending on `PrivateKeyUnlocked`. While the key is
unlocked, the passphrase field is disabled.

The passphrase is handled as follows. The field's text is copied to a character array and the field
is emptied. The page passes the array to the command and overwrites it with zeros after the
command has finished. The field's label states the largest length, 256 characters, which is the
size limit of a keyring entry (Chapter 6).

If the passphrase is wrong, the vertex cannot be signed, `PrivateKeyUnlocked` stays `false`, and
the *Unlock* button stays in place. The page then shows the message "The key could not be
unlocked. Check the passphrase." (Section 16.7.4). The passphrase itself is never written to the
log (Chapter 18).

## 16.6 The contact card

The contact card shows the data a client needs in order to use the node: the node's address, its
onion service, and a QR code. The QR code holds the hostname, the onion service, the address and
the public key as JSON (Chapter 6). It is created on the node with the library `QRCoder` and placed
in the page as an image.

The card reads its data once, when the page is opened. If the key files or the settings `Hostname`
and `OnionService` change, the page must be loaded again.

## 16.7 The peers card

### 16.7.1 The list

The list combines the two peer values of `DashboardUIState`. Two entries are treated as the same
peer if they have the same address.

**Table 16.4:** Entries of the peers list.

| Peer is | Label | Delete button |
|---|---|---|
| stored in `Peers`, and both nodes list each other as neighbor | *Connected* | Yes |
| stored in `Peers`, but one of the two does not list the other | *Disconnected* | Yes |
| not stored in `Peers`, and both nodes list each other as neighbor | *Connected*, marked *incoming* | No |

The label comes from the network graph. A peer is shown as *Connected* when two conditions hold:
the node has received a vertex in which that peer names this node as its neighbor, and the node's
own vertex names the peer. The second condition follows the live sessions (Chapter 8), so it
becomes false as soon as a connection closes. With the first condition alone, as before 5.1.0, a
peer stayed *Connected* for 6 to 11 minutes after a lock or after its removal, until the cleanup of
the graph removed its last vertex (Chapter 10).

The list is brought up to date whenever the graph or the node's own vertex changes: when a vertex
is added, replaced or removed, when a neighbor is added to or removed from the own vertex, and when
the own vertex is built again after a lock or an unlock.

The third row covers one-sided peering: another node has added this node as its peer, but not the
other way round (Chapter 11). Such an entry is marked *incoming* and cannot be removed here,
because it is not stored on this node. Its host is taken from the other node's vertex: the
hostname, or the onion service if there is no hostname.

A peer that stops without closing its connection is noticed only when the connection times out,
after about 30 seconds on a local network. Until then the label still reads *Connected*.

The stored peers are loaded into `DashboardUIState` each time the network bridge is started. The
bridge is first started when the key is unlocked. On a node that has been locked since it started,
the list is therefore empty, even if peers are stored.

### 16.7.2 Actions

**Table 16.5:** Actions on the peers card.

| Action | Command | Effect |
|---|---|---|
| *Add* | `AddPeerCommand` with host and address | Stores the peer and starts the bridge. The address must be a valid address that is not stored yet, and the host an absolute URL (Chapter 11). |
| Delete button | `RemovePeerCommand` with the peer's `Id` | Deletes the peer and starts the bridge, which closes the connection. |
| *Retry* | `InvokeNetworkBridgeCommand` | Starts the bridge, which tries again to connect to every peer that is not connected. |

The form and the *Retry* button are disabled while the key is locked, because the bridge cannot
sign in to a peer without the key.

If the address or the host is not valid, nothing is stored and the list stays as it was. The same
holds when a peer with the same address is already stored: an address can be stored only once. In
each of these cases the page shows a message (Section 16.7.4) and the two input fields keep their
content, so that the entry can be corrected. Spaces before and after the two values are removed.

### 16.7.3 Instructions for the operator

A stored peer cannot be edited. To change it, the entry is removed and a new one is added.

**Adding a peer.** Enter the base URL of the peer's public endpoint as host, for example
`http://….onion`, and the peer's address. Both must come from the operator of the peer, through a
channel that can be trusted. The entry is shown as *Connected* once the peer lists this node as its
neighbor, which normally takes a few seconds.

**Replacing a peer whose address or host has changed.** The order matters:

1. Remove the old entry with its delete button.
2. Add the new entry.

If the new entry is added first, there are two entries for the same host for a while. The bridge
uses only one of them, and it may be the old one. The connection then fails although the correct
entry is stored. Removing the old entry repairs this.

**A peer that stays *Disconnected*.** Check, in this order:

1. that the key is unlocked; without it the bridge does not run;
2. that the host can be reached, through Tor for a `.onion` host (Chapter 11);
3. that the address is the peer's current one; if the peer has replaced its key, the log shows
   "Unexpected target address", and the entry must be replaced as described above;
4. that no second entry for the same host is stored.

*Retry* starts the bridge at once instead of waiting for its next run.

**Locking a node that other nodes connect to.** A locked node closes the connections it has opened
itself, and the peers at the other end show it as *Disconnected* at once. The nodes that have
connected *to* it are not told. They keep their connection and keep showing the locked node as
*Connected*, although it cannot route messages while it is locked (Chapter 20). Nothing has to be
done on those nodes; unlocking the key restores normal operation.

### 16.7.4 Messages

`ToastHost` shows short messages (*toasts*) in the top right corner of the page. A message
disappears after six seconds or when its close button is pressed. It is shown only on the page
where the action was made. The component uses the toast styles of Bootstrap and needs no script:
the page itself adds and removes the messages.

**Table 16.6:** Messages of the dashboard.

| Action | Outcome | Message |
|---|---|---|
| *Unlock* | The key cannot be used | The key could not be unlocked. Check the passphrase. |
| *Lock* | The key is still usable afterwards | The key could not be locked. |
| *Add* | A field is empty | Enter the host and the address of the peer. |
| *Add* | The host is not an absolute URL | The host must be a full address, for example http://example.onion. |
| *Add* | The address is not a valid address | The peer address is not valid. |
| *Add* | The command fails, normally because the address is already stored | The peer could not be added. Check that its address is not already in the list. |
| *Add* | The peer is stored | Peer added. (green) |
| Delete button | The command fails. A peer that is already gone, for example because it was removed on another open page, is not a failure: the list is brought up to date without a message. | The peer could not be removed. |

The page makes the checks on the host and the address itself, with the same rules as
`AddPeerHandler` (Chapter 11), so that it can name the reason. A successful unlock, lock or removal
shows no message, because the page itself shows the result.

## 16.8 Limits of the design

### 16.8.1 The Blazor script

The script `_framework/blazor.web.js` is not part of `wwwroot`. It is provided by the .NET SDK as a
*static web asset*. In published output the file is copied to `wwwroot/_framework`. An unpublished
project, started with `dotnet run`, loads such assets by itself only in the `Development`
environment. `App.cs` therefore calls `UseStaticWebAssets()`, which loads them in every
environment.

Without the call, a node started with `dotnet run` in `Production` would not serve the script: the
dashboard would be shown but would not react.

### 16.8.2 Other limits

- **No sign-in.** Access depends only on who can reach the control endpoint (Chapter 14).
- **Reload after a restart of the node.** A page that was open while the node restarted must be
  loaded again. The keys that protect the page's form data are kept in memory only, so the
  restarted node cannot read what the old page sends, and the log gets one antiforgery error.
- **Small windows.** In a window less than about 800 pixels high, the form for a new peer starts
  below the visible area; the page scrolls to it.
- **The passphrase crosses the connection.** It is typed in the browser and sent to the node over
  the Blazor connection. The dashboard should be opened over the loopback address, an onion
  service, or another protected connection.
- **Shared state.** All open dashboard pages share one `DashboardUIState` and show the same
  values. This is intended, since there is one operator.

## 16.9 Adding a part to the dashboard

1. If the new part shows data that changes while the page is open, add the value, its `Set...Async`
   method and its change event to `DashboardUIState`, following Listing 16.1. Call the method from
   the component that owns the data.
2. Create a component in `UI/Dashboard/`. Subscribe to the event in `OnInitializedAsync`,
   unsubscribe in `Dispose`, and use `InvokeAsync` in the event handler.
3. Let the component report actions to the page through `EventCallback` parameters. Send the
   command from `Dashboard.razor`, and put the logic into a MediatR handler, not into the page.
4. Do not put anything into `wwwroot` that only the operator may see (Chapter 14).

## 16.10 Summary

The dashboard is a single Blazor Server page on the control endpoint. Its components run on the
node, use the node's services directly, and are updated by the node when its state changes. The
page holds no logic: it sends the same MediatR commands as the rest of the node, to unlock or lock
the key and to add or remove peers. `DashboardUIState` carries the key state and the peer lists
from the components that own them to every open page. A peer is shown as *Connected* when it and
this node list each other as neighbor in the network graph. A failed action is reported by a short
message on the page. The page has no sign-in.
