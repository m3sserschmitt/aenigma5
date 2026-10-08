# 12. HTTP API internals

**Abstract.** Besides the hub, a node offers a small HTTP API for node information, the network
map, shared data and files. [`API.md`](../API.md) describes each endpoint from the caller's side.
This chapter describes how the endpoints are mapped and how handler results become HTTP status codes.
It then explains how shared data and files are stored, checked and deleted, and which size limits
apply.

## 12.1 Endpoint mapping

The endpoints are ASP.NET Core minimal APIs. `StartupConfiguration.Configure` maps them, together
with their OpenAPI descriptions, and the static class `Api` (`Enigma5.App/Api.cs`) contains the
code behind them. Each method in `Api` checks its input and sends one MediatR query or command
(Chapter 2).

**Table 12.1:** HTTP endpoints and the MediatR requests they send.

| Endpoint | MediatR request |
|---|---|
| `GET /`, `GET /Info` | `GetServerInfoQuery` |
| `GET /Vertices` | `GetVerticesQuery` |
| `GET /Vertex?address=` | `GetVertexQuery` |
| `GET /LocalVertex` | `GetVertexQuery`, with the node's own address |
| `POST /Share` | `CreateSharedDataCommand` |
| `GET /Share?tag=` | `GetSharedDataQuery` |
| `PUT /IncrementSharedDataAccessCount?tag=` | `IncrementSharedDataAccessCountCommand` |
| `POST /File` | `CreateFileCommand` |
| `GET /File?tag=` | `GetFileQuery` |
| `PUT /IncrementFileAccessCount?tag=` | `IncrementFileAccessCountCommand` |

Each request is handled by the class with the same name, with `Query` or `Command` replaced by
`Handler` (for example `GetVertexHandler`), in `Resources/Handlers`. Its file has the same name as
the class, except `IncrementFileAccessCountHandler`, which is in `IncrementFileAccessCount.cs`.

All endpoints are open: none requires signing in. Access to them can only be limited per endpoint
with the setting `HttpBlacklists` (Chapter 14). JSON responses leave out properties whose value is
`null`.

## 12.2 From handler result to HTTP status

Handlers return a `CommandResult<T>` with a value and a success flag. Three helper methods in
`CommandResultExtensions` turn it into an HTTP response.

**Table 12.2:** How handler results become HTTP status codes.

| Helper | Used by | Rules |
|---|---|---|
| `CreateGetResponse` | `GET /Info`, `/Vertices`, `/Vertex`, `/LocalVertex`, `/Share` | Failure: 500. Success with a value (or any list, even empty): 200. Success without a value: 404. |
| None; the same rules, written out in `Api.GetFile` | `GET /File` | Failure: 500. Success with an open file: 200, with the file as the body. Success without a value: 404. |
| `CreatePostResponse` | `POST /Share`, `POST /File` | Success with a value: 200. Otherwise: 500. |
| `CreatePutResponse` | Both `PUT` endpoints | The value is the number of changed records. Failure: 500. Success with 1 or more: 200. Success with 0: 404. |

Some endpoints check their input before sending the request and answer `400` themselves. Tags must
be valid GUIDs, and the address of `GET /Vertex` must be a valid address. Both are converted to
lowercase before use, so they are not case-sensitive.

An unknown item is reported with 404, and the handlers follow one rule for it (Section 2.4.7): not
found is a successful result without a value. `GetVertexHandler`, `GetSharedDataHandler` and
`GetFileHandler` return such a result for an unknown address or tag. For a file this covers three
cases: no record, no file on disk, and a tag that gives no path inside the upload directory. The
handlers of the two `PUT` endpoints return a successful result with the value 0 when no item has
the given tag, which `CreatePutResponse` turns into 404. A failed result, and with it status 500,
is left for real errors.

## 12.3 Node information and the network map

`GET /Info` returns the node's public key and address, the graph version (Chapter 10) and, if set,
the settings `Hostname` and `OnionService`. `GET /Vertices` returns all vertices of the network
graph, `GET /Vertex` one vertex by address and `GET /LocalVertex` the node's own vertex. These
endpoints read copies of the graph and change nothing.

## 12.4 Shared data

Shared data is a small, signed piece of data that a client stores on a node and others read by its
tag. It is kept in the `SharedData` table (Chapter 13).

**Creating** (`POST /Share`). The request carries a public key, the signed data (data followed by
its signature, in base64; Chapter 5) and an access count (default 1). The model is validated first.
The key must be a PEM public key, the data must be base64 and signed with that key, and the access
count must be above 0. Otherwise the answer is 400, with a list of errors; a wrong signature gives
the error `The signature could not be verified.`. The handler checks the signature once more and
stores the signed data, the key and the maximum access count under a new random GUID tag. The
response contains the tag, the expiry time (now plus `SharedDataRetentionPeriod`) and a `resourceUrl`, which is built from
`Hostname`, or from `OnionService` if `Hostname` is not set. If neither is set, `resourceUrl` is
left out.

**Reading** (`GET /Share`). Returns the tag, the signed data and the public key, so that the reader
can check the signature. Reading does not change the access count.

**Counting** (`PUT /IncrementSharedDataAccessCount`). Adds one to the access count. When the count
reaches the maximum, the record is deleted at once, and a later call for the same tag is answered
with 404. Counting is the reader's responsibility; the
node does not count reads by itself.

**Expiry.** A background job deletes shared data older than `SharedDataRetentionPeriod` (14 days by
default) every five minutes (Chapter 17).

## 12.5 Files

Files are stored as plain files in the directory `WebContentDirectory`, named after their tag, with
a record of each file in the `Files` table (Chapter 13).

**Uploading** (`POST /File`). A `multipart/form-data` request with the fields `file` and
`maxAccessCount`. Antiforgery checks are turned off for this endpoint, since it is called by apps,
not by web pages. The handler creates the directory if needed, stores a record with a new random
GUID tag, and then writes the file under that name. If the file cannot be written, the record is
removed again and the answer is 500 (Chapter 13). The response contains the tag, the expiry time
and a `resourceUrl`, built as for shared data.

`maxAccessCount` must be at least 1, as for shared data; a missing file, an empty file or a smaller
count is answered with 400 before anything is written.

**Downloading** (`GET /File`). The tag is turned into a file path in a safe way: only the parsed GUID
is used as the file name, and the path must lie inside `WebContentDirectory`. The file must exist and
have a record; otherwise the answer is 404. The file is returned as `application/octet-stream`,
named after its tag. Reading does not change the access count.

**Counting** (`PUT /IncrementFileAccessCount`). As for shared data; when the count reaches the
maximum, the record and the file are deleted.

**Expiry.** A background job deletes files older than `FilesRetentionPeriod` (3 days by default),
with their records, every five minutes.

## 12.6 Size limits

**Table 12.3:** Size limits of the HTTP API.

| Endpoint | Limit | Setting | Over the limit |
|---|---|---|---|
| `POST /Share` | Whole request body: 16 KiB by default | `SharedDataMaxSize` | 413 |
| `POST /File` | Whole request body and multipart body: 64 MiB by default | `SharedFileMaxSize` | 413; nothing is stored |

Both limits are read once at startup, when the endpoints are mapped.

## 12.7 OpenAPI and Swagger

In the `Development` environment, the node publishes its OpenAPI document at `/openapi/v1.json` and
Swagger UI at `/swagger`. The descriptions come from the `WithDescription` calls in
`StartupConfiguration` and from the `[Description]` attributes on the request parameters and data
objects. [`API.md`](../API.md) was written from this document.

## 12.8 Summary

The HTTP API consists of minimal API endpoints that check their input, send one MediatR request
each, and turn the result into a status code with three helpers. None of them requires signing in;
access is limited only per endpoint by configuration. Shared data is stored after its signature has
been checked and is returned with its key so that readers can check it too. Files are stored under
their tag in the upload directory, with a safe mapping from tag to path. Both are deleted when their
access count reaches the maximum, which readers must increase themselves, or when they expire.
Invalid input, including a wrong signature or an access count below 1, is answered with 400, and
unknown items with 404, by the `GET` and the `PUT` endpoints alike.
