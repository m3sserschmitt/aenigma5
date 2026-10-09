# 13. Data layer

**Abstract.** A node keeps four kinds of records in a SQLite database: pending messages, shared
data, file records and peers. This chapter describes the tables, how the database is opened and
configured, and how its structure is changed with migrations. It then explains the two paths to
the database: reads through a `DbContext` of the current request, and writes through a single
writer thread. It shows why a change that depends on a stored value must be read and written in
one step of the writer. It ends with the limits of the design and with the rules a contributor
must follow when adding a table or a write operation.

## 13.1 Overview

The data layer uses Entity Framework Core with SQLite. Its code is in three places:

- `Enigma5.App/Data/` holds the entity classes and `EnigmaDbContext`;
- `Enigma5.App/Migrations/` holds the migrations that create and change the tables;
- `Enigma5.App/Resources/Handlers/` holds the database writer (`DbWriter`,
  `SingleThreadDbWriter`) and the MediatR handlers that read and write records.

The network graph and the sessions are not stored in the database. They are kept in memory only
(Chapters 8 and 10).

## 13.2 Tables

`EnigmaDbContext` defines four tables. All entity classes derive from `Entity`, which adds two
columns to each table: `DateCreated` (date and time, as text) and `Timestamp` (the same moment in
Unix seconds). The cleanup jobs compare `Timestamp` values, because whole numbers are compared
faster and more reliably than dates stored as text.

**Table 13.1:** Tables of the database.

| Table | Entity class | Key | Other columns | Described in |
|---|---|---|---|---|
| `Messages` | `PendingMessage` | `Id`<br>(number, set by the database) | `Destination`<br>`Content`<br>`Uuid`<br>`Sent`<br>`DateSent`<br>`SentTimestamp` | Chapter 9 |
| `SharedData` | `SharedData` | `Tag`<br>(GUID) | `Data`<br>`PublicKey`<br>`AccessCount`<br>`MaxAccessCount` | Chapter 12 |
| `Files` | `FileRecord` | `Tag`<br>(GUID) | `AccessCount`<br>`MaxAccessCount` | Chapter 12 |
| `Peers` | `Peer` | `Id`<br>(number, set by the database) | `Host`<br>`Address` | Chapter 11 |

The `Files` table holds only a record of each file. The file content is stored in the directory
`WebContentDirectory`, in a file named after the tag (Chapter 12).

The `Messages` table has two indexes:

- on `Destination` and `Sent`, used when a recipient collects or confirms its messages;
- on `Uuid`, used by the check for a message that was already received (Chapter 9).

The index on `Uuid` is not unique; the writer makes sure that a `uuid` given by a caller is stored
only once (Section 13.6.3). The tables have no relations between them, so there are no foreign
keys.

## 13.3 Opening the database

The setting `DbProvider` selects the database type. `Sqlite` is the only value, and it is also used
when the setting is missing or not recognized. The connection string is read from
`ConnectionStrings:DbConnectionString`. By default it names the file `aenigmaDb.sqlite` in the
current working directory.

`SqlitePragmaInterceptor` runs each time a connection to the database is opened. It applies the
three settings of Table 13.2.

**Table 13.2:** SQLite settings applied to every connection.

| Setting | Effect |
|---|---|
| `journal_mode=WAL` | Changes are first written to a separate log file. Readers are not blocked while a write is in progress. |
| `synchronous=NORMAL` | The database waits for the disk less often. This is faster and, in WAL mode, still keeps the database consistent after a crash. |
| `foreign_keys=ON` | Foreign keys are checked. No table uses one at present. |

If WAL mode cannot be turned on, the interceptor logs a critical error and the connection fails.
In WAL mode, SQLite keeps two more files next to the database file, with the endings `-wal` and
`-shm`. A backup must copy all three files, or be made while the node is stopped.

## 13.4 Migrations

The structure of the database is described by EF Core migrations. At startup, `MigrateDatabase`
applies every migration that the database does not have yet (Chapter 4). A new node therefore
creates its database by itself, and an updated node upgrades it at its first start. If a migration
fails, the error is logged and the node stops.

**Table 13.3:** Migrations, in the order they are applied.

| Migration | Change |
|---|---|
| `InitialMigration` | Creates `Messages`. |
| `AddSharedDataTable` | Creates `SharedData`. |
| `ChangeDateTimeColumnsToDateTimeOffsets` | No change to the tables. |
| `AddAccessCountForSharedData` | Adds the access counters to `SharedData`. |
| `AddAuthorizedServicesTable` | Creates `AuthorizedServices` (removed later). |
| `AddPublicKeyToSharedData` | Adds `PublicKey` to `SharedData`. |
| `AddDateSentOnPendingMessage` | Adds `DateSent` to `Messages`. |
| `AddUuidOnPendingMessages` | Adds `Uuid` to `Messages`. |
| `AddFileRecords` | Creates `Files`. |
| `DbRefactoring` | Renames `DateReceived` to `DateCreated` and adds the `Timestamp` columns. |
| `AddPeersTable` | Creates `Peers`, removes `AuthorizedServices`, and removes the file content from `Files`. |
| `AddIndicesOnDestinationAndUuidForPendingMessages` | Adds the two indexes on `Messages`. |

## 13.5 Reading

Handlers that read from the database receive an `EnigmaDbContext` through their constructor. The
context is created for the current request: one HTTP request, one hub call, or one background job.
Reads run on the thread of that request and do not wait for the writer.

Queries are written with LINQ. Where a condition depends on the request, handlers build it with
`PredicateBuilder` from the library LinqKit. `GetPendingMessagesByDestinationHandler`, for
example, always selects the pending messages of one destination, and adds the condition
`Id > infId` only if the caller gave a value (Chapter 9).

Handlers return data objects (`...Dto`) from `Enigma5.App.Models`, not entity objects. Entity
objects stay inside the data layer.

## 13.6 Writing

### 13.6.1 One writer

SQLite allows only one write at a time. If two connections write at once, one of them waits and
may fail with the error "database is locked". To avoid this, all writes go through one component,
the database writer, which runs them one after another on its own thread (Chapter 3).

The writer is defined in three parts:

- `IDbWriter` (`Resources/Contracts/`) lists the write operations;
- `DbWriter` is an abstract base class that checks the cancellation token and then calls the
  matching `Run...` method;
- `SingleThreadDbWriter` implements the `Run...` methods for SQLite.

`SingleThreadDbWriter` is registered once for the whole node (singleton) and owns a
`SimpleSingleThreadRunner`. Each operation is one work item on the runner's thread.

**Listing 13.1:** Shape of every write operation in `SingleThreadDbWriter`.

```text
on the writer thread:
    create a new scope and take a new EnigmaDbContext from it
    make the change
    save                    (SaveChanges, ExecuteUpdate or ExecuteDelete)
    dispose the scope
return the number of changed rows to the caller
```

The writer uses a new context for each operation, and not the context of the request. A context
must not be used by two threads at the same time, and the writer thread outlives every request.

If a write fails, the runner logs the error and the caller receives the exception. Handlers do not
catch it. `RequestResponseLoggingBehavior`, which wraps every MediatR request, logs it and turns it
into a failed `CommandResult` (Chapter 2).

### 13.6.2 Write operations

**Table 13.4:** Write operations of `IDbWriter`.

| Operation | Change | Used by |
|---|---|---|
| `CreatePendingMessageAsync` | Adds one message. On request, first looks for a stored message with the same `uuid` and returns that one instead (Section 13.6.3). | `RouteMessage` (Chapter 9) |
| `MarkMessagesAsDeliveredAsync` | Marks all messages that match a condition as delivered, with one statement. | `Cleanup`, `Cleanup2` |
| `RemoveMessagesAsync` | Deletes all messages that match a condition, with one statement. | Cleanup job (Chapter 17) |
| `CreateSharedDataAsync` | Adds one shared data record. | `POST /Share` |
| `IncrementSharedDataAccessCountAsync` | Reads the record by its tag and adds one to the access count, or deletes the record when the maximum is reached (Section 13.6.3). | `PUT /IncrementSharedDataAccessCount` |
| `RemoveSharedDataAsync` | Deletes all shared data that matches a condition. | Cleanup job |
| `CreateFileAsync` | Adds one file record. | `POST /File` |
| `IncrementFileAccessCountAsync` | As for shared data. | `PUT /IncrementFileAccessCount` |
| `RemoveFileAsync` | Deletes one file record. | Cleanup job |
| `CreatePeerAsync`, `RemovePeerAsync` | Adds or deletes one peer. | Dashboard (Chapter 16) |

Operations that change many rows take a condition and run as a single SQL statement
(`ExecuteUpdate`, `ExecuteDelete`). The rows are not loaded into memory first.

### 13.6.3 Changes that depend on a stored value

Some changes depend on what is stored: an access count is raised by one, and a message is added
only if its `uuid` is not stored yet. If a handler read the value with the context of the request
and then asked the writer to change it, another request could change the same record between the
two steps.

These operations therefore read and write inside the same work item of the writer. Work items run
one after another, so nothing can happen between the read and the write.

- The two increment operations take a tag. The writer reads the record, raises the count, and
  stores or deletes the record. It returns the record with its new count, or nothing if the tag is
  unknown.
- `CreatePendingMessageAsync` takes a flag. If the flag is set, the writer first looks for a
  message with the same `uuid` and returns it if one exists. Otherwise it adds the new message.

## 13.7 Limits of the design

### 13.7.1 Files and their records

A file and its record are stored in two places, so they cannot be written as one unit. The code
keeps one rule instead: the record is stored first and the file second. Every file on disk
therefore has a record, and the cleanup job can always find and delete it.

If writing the file fails, for example because the disk is full, `CreateFileHandler` deletes what
was written of the file and removes the record again, logs the error and reports a failure.

Two gaps remain, both listed in Chapter 20:

- If the node stops between the two steps, the record stays, perhaps with a part of the file,
  until the cleanup job deletes it. Nobody has received its tag.
- Deleting works in the other order when an access count reaches its maximum: the record is
  removed first and the file second. If deleting the file fails, the file stays without a record,
  and the cleanup job does not find it.

### 13.7.2 One process per database

The writer makes writes safe only inside one process. Two nodes must not use the same database
file.

## 13.8 Rules for contributors

**Adding or changing a table.** Change the entity class or add a new one with its `DbSet` in
`EnigmaDbContext`. Then create a migration in `Enigma5.App/`:

**Listing 13.2:** Creating a migration.

```bash
cd Enigma5.App
dotnet ef migrations add <Name>
```

The command needs the `dotnet-ef` tool. The new migration is applied when the node next starts.
Migrations that were already released must not be edited; a later change needs a new migration.

**Adding a write operation.**

1. Add the method to `IDbWriter`.
2. Add the public method and the abstract `Run...` method to `DbWriter`.
3. Implement the `Run...` method in `SingleThreadDbWriter`, with the shape of Listing 13.1.
4. Call the new method from a handler. A handler must never call `SaveChanges` on its own context.

**Changing a value that depends on the stored value.** Do the read and the change inside the same
work item of the writer, or in one SQL statement (Section 13.6.3). Never read the value in the
handler and pass it to the writer.

## 13.9 Summary

A node stores pending messages, shared data, file records and peers in a SQLite database, which it
creates and upgrades by itself at startup. Every connection uses WAL mode, so reads are never
blocked by a write. Reads use the `DbContext` of the current request. All writes go through one
writer, which runs them one after another on its own thread and with its own context. This avoids
"database is locked" errors. Changes that depend on a stored value, such as access counts and the
check for a `uuid` that is already stored, are read and written in one step of the writer, so
requests that arrive at the same time cannot disturb each other.
