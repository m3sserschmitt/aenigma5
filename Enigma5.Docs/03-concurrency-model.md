# 3. Concurrency model

**Abstract.** ASP.NET Core handles many HTTP requests and hub calls at the same time, on many
threads. Some components of a node, however, own data that must not be changed by several threads
at once. This chapter describes how the node solves this problem with a *single-thread runner*
and states the rules the runner guarantees. It lists the six components that use a runner and
explains why each needs one. It then describes three consequences of how the runner is written, and gives
rules for code that depends on it.

## 3.1 Motivation

A node serves many clients and peers at the same time. Some of its components keep data that many
requests change, and this data is not safe to change from several threads at once. Examples are
plain dictionaries and sets, several-step updates of the network graph, a database that allows
only one writer at a time, and a native library that handles the passphrase on one thread only.

The node does not protect this data with locks. Instead, it gives each such piece of data to one
component, and that component runs all of its operations one after another on its own thread. The
problem of concurrent access then becomes a simple matter of the order of a queue.

## 3.2 The single-thread runner

### 3.2.1 How it works

The class `SimpleSingleThreadRunner` (`Enigma5.App.Common/Utils/SimpleSingleThreadRunner.cs`) is a
work queue served by one thread:

- When it is created, it starts one background thread. This thread takes *work items* from a
  `BlockingCollection<Func<Task>>` and runs them in the order they were added.
- The method `RunAsync` adds a work item to the queue. It returns a `Task<T>` that completes with
  the result of the work item. One version of the method takes a synchronous function
  (`Func<T>`), the other an asynchronous one (`Func<Task<T>>`).
- If a work item throws an exception, the exception is logged (if a logger was given) and stored
  in the returned task, so the caller sees it when it awaits the task. The thread then goes on
  with the next work item.
- `Dispose` stops accepting new work items and waits until the thread has run the ones already in
  the queue.

The runner is registered as a transient service. So every component that asks for a runner gets
its own thread.

### 3.2.2 Guarantees

Listing 3.1 states what a runner guarantees. For a work item `w`, `enq(w)` is the time it is added
to the queue, and `start(w)` and `end(w)` are the times it starts and finishes. For an asynchronous
work item, `end(w)` is the time the task it returns completes.

**Listing 3.1:** Guarantees of a single-thread runner R.

```text
For all work items w, w' run by R:

  (FIFO)       enq(w) < enq(w')   implies   start(w) < start(w')
  (Exclusion)  enq(w) < enq(w')   implies   end(w) <= start(w')
  (Affinity)   the synchronous part of w up to its first await runs on R's thread
  (Release)    a caller awaiting w resumes on the thread pool, not on R's thread
```

*Exclusion* means that the operations of a component that owns a runner never overlap. The data
they use therefore needs no other protection. *Affinity* is needed for the key thread
(Section 3.3). The parts of an asynchronous work item that come after an `await` may run on
another thread. Exclusion still holds, because R does not start the next work item until the
current one has fully completed (Section 3.4.1). *Release* makes sure that only work items use the
runner's thread, so the code of callers never slows a runner down (Section 3.4.2).

## 3.3 Components that own a runner

Table 3.1 lists the six components that own a runner and why each needs to run its work one item
at a time.

**Table 3.1:** Components that own a single-thread runner.

| Component | Why its work must run one item at a time |
|---|---|
| `CertificateManager` (key thread) | The native library keeps the handle of the passphrase in one variable for the whole process. In `Ephemeral` mode, the passphrase is stored in the keyring of the thread that created it. So looking up, storing and removing the passphrase, and creating decryptors and signers, must all happen on one thread, one at a time (Chapter 6). |
| `SessionManager` | Pending challenges, signed-in connections and the map from addresses to connections are kept in plain dictionaries and sets (Chapter 8). |
| `NetworkGraph` | The set of vertices and the local vertex are updated in several steps (read, change, write) that must not be mixed with other updates (Chapter 10). |
| `SingleThreadDbWriter` | SQLite allows only one writer at a time. Running all writes one after another avoids "database is locked" errors (Chapter 13). |
| `Bridge` | Starting the connections to peers and removing failed ones both change the same set of connections (Chapter 11). |
| `DashboardUIState` | Changes of the values shown on the dashboard must reach the open pages in the order in which they were stored (Chapter 16). |

All six components are singletons, so a running node has exactly six runner threads.

Components that own a runner offer asynchronous methods, and each method is one work item. The
only exception is `DashboardUIState`, whose three values can also be read directly: they are
written only by its runner and are replaced as a whole, never changed in place (Chapter 16).
`NetworkGraph` also returns deep copies of its data, made by serializing and deserializing it. This
way, no caller holds a reference to an object that the runner thread might change later.

## 3.4 Consequences of the design

Three consequences follow from how the runner is written.

### 3.4.1 Asynchronous work items keep the queue busy

The runner waits for the task of an asynchronous work item to finish. While the work item waits
for input or output, its continuation runs on the thread pool, but the runner does not start the
next work item until the task has completed. So a work item that needs only microseconds still has
to wait for any slow work item in front of it. Slow input and output should therefore happen
before a work item is queued, unless that input or output itself must run one at a time. For
example, `SessionManager.AuthenticateAsync` gets the node's own address before it queues its work
item.

### 3.4.2 Where the caller continues

For a synchronous work item, the runner completes the returned task on its own thread, by calling
`SetResult` on a `TaskCompletionSource`. By default, .NET runs the code that was waiting for a task
inside the call that completes the task. The caller of `RunAsync` would then continue on the
runner's thread and keep it busy until its next wait. To prevent this, the runner creates each
`TaskCompletionSource` with `TaskCreationOptions.RunContinuationsAsynchronously`. `SetResult` then
hands the caller's code to the thread pool, and the runner thread goes straight back to its queue.
Only the work items themselves run on the runner thread.

This matters most for the key thread. When `OnionParser` awaits
`CertificateManager.CreateUnsealerAsync()`, only the creation of the decryptor runs on the key
thread; this is the step that reads the passphrase from the keyring. Decrypting the onion
afterwards, the most expensive step of routing, runs on the thread pool. Onions that arrive at the
same time are therefore decrypted in parallel. The same applies to the session
thread: only the dictionary operations of `SessionManager` run on it, not the rest of the hub call
that waited for them.

Because caller code never runs on a runner thread, a blocking call in caller code can no longer
make a runner wait for itself. Such calls should still be avoided (rule R3).

### 3.4.3 A work item must not wait for its own runner

A work item must never wait for another work item of the same runner. The second item would be
queued behind the first one, which would wait for it forever. Waiting for a work item of a
*different* runner is allowed, and the code does this in a few places, listed in Table 3.2.

**Table 3.2:** Runners that wait for other runners.

| Work item of | Waits for | Example |
|---|---|---|
| `NetworkGraph` | `CertificateManager` | Signing a new local vertex |
| `NetworkGraph` | `DashboardUIState` | Storing the new list of inbound peers after the graph changed |
| `Bridge` | `CertificateManager` | Signing the sign-in challenges of peers; checking that the key can sign |
| `Bridge` | `NetworkGraph`, `DashboardUIState` | Bringing the local vertex and the shown key state into line when the key was locked or unlocked from outside |
| `Bridge` | Local and remote hubs over SignalR, whose handlers use `SessionManager` and `NetworkGraph` | Starting and signing in the connections to peers |

These waits never form a cycle, so runners cannot block each other. The work item of `Bridge` runs
longer than any other: it opens connections, signs in, sends pending messages and triggers a
broadcast, possibly over Tor. Other operations of `Bridge` wait behind it, but no other runner
does.

## 3.5 Rules for contributors

The behavior above leads to the following rules for code that uses runners.

- **R1.** Data shared between requests belongs to a component that owns a runner. Only that
  runner's work items change it.
- **R2.** Work items stay short. File, network and vault access happens before the work item is
  queued, unless the access itself must run one at a time.
- **R3.** Work items never block on a task (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`),
  because that stops the runner. A work item never waits for a work item of the same runner.
- **R4.** A component whose data is changed on its runner thread returns copies of that data, not
  references to it.
- **R5.** New waits between runners must not create a cycle in Table 3.2.
- **R6.** A new component that needs to run its work one item at a time asks for a
  `SimpleSingleThreadRunner` in its constructor, and thereby gets its own thread.

## 3.6 Summary

The node protects shared data by giving it to a component that runs its operations on its own
thread, through `SimpleSingleThreadRunner`. The runner guarantees first-in-first-out order, that
work items never overlap, and that callers continue on the thread pool. Six components use it:
the certificate manager, the session manager, the network graph, the database writer, the
network bridge and the dashboard state. Because callers continue on the thread pool, only work items use a runner thread,
and onions are decrypted in parallel outside the key thread. An asynchronous work item keeps the
queue busy until it finishes, and a work item that waits for its own runner blocks forever. Rules
R1 to R6 avoid these problems.
