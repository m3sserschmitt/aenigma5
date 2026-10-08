# 17. Background jobs

**Abstract.** A node does some of its work without a request from outside: it deletes old records,
it keeps the network map up to date, and it keeps the connections to its peers alive. This work is done by five repeating jobs, which
are scheduled with the library Hangfire. This chapter describes how Hangfire is set up, how jobs
can be observed, how a job is turned into a MediatR command, what each job does, and when it runs.
It also describes what happens when a job fails or when the node restarts, and how a new job is
added.

## 17.1 Overview

All jobs follow the same pattern. A job does not contain logic. It sends one MediatR command, and
the handler of that command does the work (Chapter 2). The same handlers can therefore also be used
from the dashboard or from other handlers.

**Table 17.1:** Background jobs.

| Job name | Command | Registered | Purpose |
|---|---|---|---|
| `messages-cleanup` | `CleanupMessagesCommand` | At startup | Deletes old pending messages and confirmed messages (Chapter 9). |
| `shared-data-cleanup` | `CleanupSharedDataCommand` | At startup | Deletes old shared data (Chapter 12). |
| `files-cleanup` | `CleanupFilesCommand` | At startup | Deletes old files and their records (Chapter 12). |
| `graph-cleanup` | `CleanupGraphCommand` | At startup | Removes expired and unlisted vertices from the network graph (Chapter 10). |
| `invoke-network-bridge` | `InvokeNetworkBridgeCommand` | After a successful key setup | Starts the network bridge (Chapter 11). |

All five jobs run every five minutes. Their names and schedules are constants in
`Enigma5.App.Common/Constants.cs` and cannot be configured.

## 17.2 Hangfire setup

Hangfire is set up by `SetupHangfire` in `Enigma5.App/Extensions/ServiceCollectionExtensions.cs`.

- **Storage.** Jobs and schedules are kept in memory (`Hangfire.InMemory`). Nothing is written to
  the database or to disk.
- **Server.** `AddHangfireServer` starts a Hangfire server inside the node's process. It runs the
  jobs on its own worker threads.
- **Serialization.** Job arguments are stored as JSON with type names (`TypeNameHandling.All`).
  This is needed because a job argument is declared as the general type `IRequest<T>`, and
  Hangfire must know the real command type to build it again.

## 17.3 Observing jobs

Every run of a job is written to the log (Chapter 18).

In the `Development` environment, the node also maps Hangfire's own web page at `/Jobs`. It shows
the repeating jobs with their schedule, their last and next run, and the history of single
runs. The page is read-only: it offers no way to start or delete a job. Because jobs are kept in
memory, the history is empty after every restart.

The page is not mapped in `Production`, and therefore not on a node installed from a package.
There are two reasons. Hangfire protects the page only by accepting local requests, and behind an
onion service or a reverse proxy every request is local. The page also shows the host name of the
machine, which a node reached through an onion service must not reveal.

The development configuration blocks `/Jobs` on the public endpoint (Chapter 14), so the page is
opened through the control endpoint, at `http://127.0.0.1:8081/Jobs`.

## 17.4 From job to command

Hangfire calls a method on an object. The object is `MediatorHangfireBridge`
(`Enigma5.App/Hangfire/`), which has one method.

**Listing 17.1:** The bridge between Hangfire and MediatR.

```text
MediatorHangfireBridge.Send(command):
    log "Executing command ... for Hangfire Job"
    send the command through MediatR
```

A job is registered with an expression that names this method and its argument, as in Listing 17.2.

**Listing 17.2:** Registration of a repeating job (shortened).

```csharp
RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
    Constants.MessagesCleanupRecurringJob,
    bridge => bridge.Send(new CleanupMessagesCommand(
        configuration.GetMessageRetentionPeriod(),
        configuration.GetSentMessageRetentionPeriod())),
    Constants.MessagesCleanupJobInterval);
```

Two points are important.

**The argument is created at registration.** The command object, with the retention periods read
from the configuration, is built once and stored with the job. Every later run uses the stored
values. A changed retention period therefore takes effect only after a restart (Chapter 15).

**Each run gets its own services.** For every run, Hangfire creates a new dependency injection
scope, takes a `MediatorHangfireBridge` from it, and disposes the scope when the run ends. The
handler and its `EnigmaDbContext` live only for that run, as they do for an HTTP request
(Chapter 13).

The node registers no activator of its own; the server started by `AddHangfireServer` creates the
scope.

## 17.5 The cleanup jobs

`StartJobs` in `StartupConfiguration` registers the four cleanup jobs at the end of the startup
(Chapter 4). They run whether the node is locked or unlocked, because they need no key.

**Table 17.2:** What the cleanup jobs delete.

| Job | Deletes | Setting |
|---|---|---|
| `messages-cleanup` | Pending messages stored longer ago than the retention period, whether collected or not | `MessageRetentionPeriod` |
| | Confirmed messages that were confirmed longer ago than the retention period | `SentMessageRetentionPeriod` |
| `shared-data-cleanup` | Shared data created longer ago than the retention period | `SharedDataRetentionPeriod` |
| `files-cleanup` | Files created longer ago than the retention period, and their records | `FilesRetentionPeriod` |
| `graph-cleanup` | Vertices other than the node's own that are expired, or that nobody lists and that are older than the grace period (Chapter 10) | `VertexLifetime`<br>`UnlistedVertexGracePeriod` |

The graph job works in memory and reads both of its settings at each run, so a change applies
without a restart. The other three jobs work on the database. Their comparisons use the
`Timestamp` columns, in Unix seconds (Chapter 13). The message and shared
data jobs each delete with one SQL statement. The file job first reads the records that are too
old, and then deletes each file from disk and its record, one after another.

A retention period of 0 means "delete at the next run". This is the default for confirmed messages:
once a recipient has confirmed a message, the node has no reason to keep it.

## 17.6 The network bridge job

The bridge job is registered later than the others. `SetMasterPassphraseHandler` registers it when
a key setup has succeeded, that is, when the node becomes unlocked (Chapter 4). At the same moment
it queues one extra run, so that the bridge starts at once and not up to five minutes later.

Each run sends an `InvokeNetworkBridgeCommand`. Its handler loads the stored peers into the
dashboard state and starts the bridge (Chapters 11 and 16). A run of the bridge on a node where all
peers are already connected does little: it finds nothing to connect and moves the messages that
are waiting.

The job is not removed when the node is locked again. It keeps running every five minutes, but
each run first checks whether the private key can be used, and does nothing if it cannot
(Chapter 11). Keeping the job has a purpose: when the key becomes usable again, the next run
connects the node to its peers without any further action.

## 17.7 Timing

- **Schedule.** The schedule `*/5 * * * *` is a cron expression. It means minutes 0, 5, 10 and so
  on of every hour, not "five minutes after the last run". A node started at 12:04 runs its first
  cleanup at 12:05.
- **Delay.** Hangfire checks its schedules at short intervals, so a run can start some seconds
  late.
- **Overlap.** Hangfire does not wait for the last run of a job to end before it starts the next.
  The cleanup jobs are short, and their writes pass through the single database writer one after
  another (Chapter 13). Runs of the bridge are serialized by the bridge's own runner (Chapter 3).

## 17.8 Failures and restarts

**Failures.** An exception in a handler does not reach Hangfire. `RequestResponseLoggingBehavior`
catches it, logs it, and returns a failed result (Chapter 2). From Hangfire's point of view the job
has succeeded, so Hangfire does not repeat it. The work is simply tried again at the next scheduled
run, five minutes later.

**Restarts.** Because the storage is in memory, all schedules are lost when the node stops. They
are registered again at the next start: the cleanup jobs always, the bridge job after the key setup
has succeeded. No run is made up for the time the node was down. This does no harm, since every
cleanup run deletes everything that is too old at that moment.

**One process.** In-memory storage belongs to one process. Jobs are not shared between nodes.

## 17.9 Adding a job

1. Write the work as a MediatR command and handler. Give the command public properties for its
   arguments, so that it can be stored as JSON.
2. Add constants for the job name and the schedule to `Constants.cs`.
3. Register the job with `RecurringJob.AddOrUpdate<MediatorHangfireBridge>`, in `StartJobs` if it
   needs no key, or next to the bridge job if it does.
4. Keep in mind that the command's arguments are fixed at registration (Section 17.4). If the job
   must see changed settings, let the handler read them from `IConfiguration` when it runs.
5. Let database changes pass through `IDbWriter` (Chapter 13).

## 17.10 Summary

Five Hangfire jobs run every five minutes. Three of them delete old messages, shared data and
files, one removes old vertices from the network graph, and the fifth starts the network bridge and exists only after the node has been unlocked. A job
holds no logic: `MediatorHangfireBridge` sends one MediatR command, and each run has its own
services, like a request. Job arguments, among them the retention periods, are fixed when a job is
registered. Schedules are kept in memory and are registered again at every start. A failing handler
is logged, not repeated, and tried again at the next run.
