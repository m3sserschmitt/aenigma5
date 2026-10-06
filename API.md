## Aenigma API Reference

**Base URL:** `http://localhost:8080/`

This document describes the HTTP API exposed by Enigma5.App for node info, shared-data storage, file storage, and network graph (vertex) queries.

---

## Table of Contents

- [Node Info](#node-info)
- [Shared Data](#shared-data)
- [Network Graph](#network-graph)
- [Files](#files)
- [Data Models](#data-models)

---

## Node Info

### `GET /`

Alias for `GET /Info`. Returns basic node info.

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns a [`ServerInfoDto`](#serverinfodto) |
| `500`  | Internal Server Error |

---

### `GET /Info`

Returns basic node info.

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns a [`ServerInfoDto`](#serverinfodto) |
| `500`  | Internal Server Error |

---

## Shared Data

### `POST /Share`

Create a new shared data object.

**Request Body** — [`SharedDataCreateDto`](#shareddatacreatedto) *(required)*

The signature in `signedData` is checked against `publicKey` before the object is stored.

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns the created [`SharedDataDto`](#shareddatadto) |
| `400`  | Invalid request; the body lists the errors. Includes `The signature could not be verified.` when `signedData` is not signed with `publicKey`, and an invalid `accessCount` (must be at least 1) |
| `413`  | Request body larger than `SharedDataMaxSize` (16 KiB by default) |
| `500`  | Internal Server Error |

---

### `GET /Share`

Returns a shared data object by its identification tag.

Reading does not change the object's access count. After a successful read, the client is
responsible for calling [`PUT /IncrementSharedDataAccessCount`](#put-incrementshareddataaccesscount).

**Query Parameters**

| Name  | Type   | Required | Description                          |
|-------|--------|----------|----------------------------------------|
| `tag` | string | Yes      | Shared data identifier in GUID format (case-insensitive) |

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns a [`SharedDataDto`](#shareddatadto) |
| `400`  | `tag` is missing or not a valid GUID |
| `404`  | No shared data with this tag: never created, expired, or maximum access count reached |
| `500`  | Internal Server Error |

---

### `PUT /IncrementSharedDataAccessCount`

Increments a shared data object's current access count. When the current access count reaches the maximum access count, the object is deleted.

**Query Parameters**

| Name  | Type   | Required | Description                          |
|-------|--------|----------|----------------------------------------|
| `tag` | string | Yes      | Shared data identifier in GUID format (case-insensitive) |

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | OK |
| `400`  | `tag` is missing or not a valid GUID |
| `404`  | No shared data with this tag: never created, expired, or maximum access count reached |
| `500`  | Internal Server Error |

---

## Network Graph

### `GET /Vertices`

Returns a list of all available nodes in the local ledger.

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns an array of [`VertexDto`](#vertexdto) |
| `500`  | Internal Server Error |

---

### `GET /Vertex`

Returns the node object identified by the given address.

**Query Parameters**

| Name      | Type   | Required | Description                                          |
|-----------|--------|----------|--------------------------------------------------------|
| `address` | string | Yes      | Vertex address in SHA-256 format, derived from its public key (64 hexadecimal characters, case-insensitive) |

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns a [`VertexDto`](#vertexdto) |
| `400`  | `address` is missing or not a valid address |
| `404`  | No node with this address in the local ledger |
| `500`  | Internal Server Error |

---

### `GET /LocalVertex`

Returns the local node's own vertex object.

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns a [`VertexDto`](#vertexdto) |
| `500`  | Internal Server Error |

---

## Files

### `POST /File`

Uploads a new file.

**Request Body** — `multipart/form-data` *(required)*

| Field            | Type    | Required | Description |
|------------------|---------|----------|-------------|
| `file`           | binary  | Yes      | The file contents |
| `maxAccessCount` | integer | Yes      | Maximum number of times the file may be downloaded; must be at least 1 |

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns the created [`SharedDataDto`](#shareddatadto) |
| `400`  | `file` is missing or empty, or `maxAccessCount` is missing or less than 1 |
| `500`  | Internal Server Error |

---

### `GET /File`

Downloads a file by its tag.

Downloading does not change the file's access count. After a successful download, the client
is responsible for calling [`PUT /IncrementFileAccessCount`](#put-incrementfileaccesscount).

**Query Parameters**

| Name  | Type   | Required | Description                    |
|-------|--------|----------|----------------------------------|
| `tag` | string | Yes      | File identifier in GUID format (case-insensitive) |

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | Returns the file as a binary stream (`application/octet-stream`), named after its tag |
| `400`  | `tag` is missing or not a valid GUID |
| `404`  | No file with this tag: never uploaded, expired, or maximum access count reached |
| `500`  | Internal Server Error |

---

### `PUT /IncrementFileAccessCount`

Increments a file's current access count. When the current access count reaches the maximum access count, the file is deleted.

**Query Parameters**

| Name  | Type   | Required | Description                    |
|-------|--------|----------|----------------------------------|
| `tag` | string | Yes      | File identifier in GUID format (case-insensitive) |

**Responses**

| Status | Description |
|--------|-------------|
| `200`  | OK |
| `400`  | `tag` is missing or not a valid GUID |
| `404`  | No file with this tag: never uploaded, expired, or maximum access count reached |
| `500`  | Internal Server Error |

---

## Data Models

### `ServerInfoDto`

| Property       | Type   | Description                          |
|----------------|--------|----------------------------------------|
| `publicKey`    | string | Server public key in PEM format |
| `address`      | string | SHA-256 hash derived from the public key |
| `graphVersion` | string | SHA-256 hash derived from the local ledger |
| `onionService` | string | Onion service base API address |
| `hostname`     | string | Base API address |

---

### `SharedDataCreateDto`

| Property     | Type    | Default | Description |
|--------------|---------|---------|-------------|
| `publicKey`  | string  | —       | Public key of the user creating the shared data, in PEM format |
| `signedData` | string  | —       | Shared data with an attached signature, in base64 format |
| `accessCount`| integer | `1`     | Maximum number of times the data can be retrieved |

---

### `SharedDataDto`

| Property      | Type     | Description |
|---------------|----------|-------------|
| `tag`         | string   | Shared data object identifier in GUID format |
| `resourceUrl` | string   | URL of the created object |
| `data`        | string   | Shared object's signed data |
| `publicKey`   | string   | Public key in PEM format used during object creation |
| `validUntil`  | datetime | Object expiration date |

---

### `VertexDto`

| Property       | Type                                  | Description |
|----------------|----------------------------------------|-------------|
| `publicKey`    | string                                  | Node public key in PEM format |
| `signedData`   | string                                  | Neighborhood serialized data with signature, in base64 |
| `neighborhood` | [`NeighborhoodDto`](#neighborhooddto) or `null` | Node neighborhood info |

---

### `NeighborhoodDto`

| Property      | Type            | Description |
|---------------|------------------|-------------|
| `address`     | string           | SHA-256 hash derived from the node's public key |
| `hostname`    | string           | Base API address |
| `onionService`| string           | Onion service base API address |
| `neighbors`   | string[]         | SHA-256 addresses of adjacent nodes |
| `lastUpdate`  | datetime         | Date and time this object was last updated |

---

*Generated from the app's OpenAPI 3.1 specification. Interactive testing is available via Swagger UI at `/swagger` in a Development environment.*