# ProjectSift Backend Technical Design

## Architecture and dependency direction

ProjectSift backend is a layered monorepo with strict inward dependencies:

- `ProjectSift.Domain` (no project references)
- `ProjectSift.Application` → references Domain only
- `ProjectSift.Infrastructure` → references Domain only
- `ProjectSift.Service` → references Application, Domain, Infrastructure

Dependency direction is:

`Service -> Application -> Domain`

`Service -> Infrastructure -> Domain`

No layer references back outward.

## Layer responsibilities

### Domain

Defines domain contracts and immutable result types:

- Batch/payload/document results and diagnostics
- Status and document enums
- Validation guards and domain exceptions
- Storage contract (`IBatchStore`)

### Application

Implements scan/extract orchestration:

- Full payload scanning over binary/noisy input
- JSON candidate boundary detection
- JSON parsing/traversal with `System.Text.Json`
- Property-path collection, deduplication, and deterministic ordering
- Batch-level processing over multiple payloads with failure isolation

### Infrastructure

Provides in-memory storage adapter:

- Shared in-memory record dictionary
- ETag conflict checks for optimistic concurrency
- Thread-safe create/read/update operations

This storage is process-local and not durable.

### Service

Hosts HTTP API and background execution:

- Upload validation and payload loading
- Batch registration and bounded queue submission
- Background processing worker and result persistence
- Error mapping and structured logging

## Scanning and extraction algorithm (high level)

1. Scan payload bytes left-to-right.
2. Start candidates only at `{` or `[`.
3. Boundary scanner tracks nesting depth, string state, and escaping.
4. Candidate outcomes:
   - Complete boundary: extract directly
   - Recoverable incomplete boundary at payload end with unambiguous closers: append deterministic closers and extract
   - Ambiguous or invalid boundary: return failed document diagnostic
5. Parse candidate with `JsonDocument.Parse` (`System.Text.Json`) with configured max depth.
6. Traverse object/array nodes to collect property paths.
7. Deduplicate paths per document (case-sensitive ordinal set), sort ordinally, and return.

Each candidate is extracted independently, so one malformed candidate does not block later candidates.

## Property path and escaping conventions

- Dot notation for simple property names: `customer.id`
- Array marker: `[]` (for example `items[]`, `items[].sku`)
- Non-simple property names are bracket-quoted with escaping:
  - `['a.b']`
  - `['x[y]']`
  - `['o\'k']`

Path deduplication is per-document and case-sensitive (`StringComparer.Ordinal`).

## Status semantics

### Batch status

- `Processing`: batch registered and pending/in-flight
- `Completed`: terminal state (success, partial failure, queue rejection, or cancellation diagnostics)

### Document status

- `Successful`: parsed and extracted
- `Recovered`: parsed after deterministic boundary repair
- `Failed`: boundary/parse/limit/exception diagnostics

## Recovery rules and limitations

Recovery is intentionally conservative:

- Recovery is attempted only when candidate ends at payload boundary with known missing closing delimiters and no string ambiguity.
- If scanner ends inside a string/escape-ambiguous state, recovery is not attempted.

Limitations:

- Arbitrary binary framing can make intended JSON boundaries ambiguous.
- Scanner never fabricates speculative documents beyond explicit delimiter-driven candidates.

## API design

### Endpoints

- `POST /v1/batches` (multipart form field: `files`)
- `GET /v1/batches/{batchId}`
- `GET /api/health`

### Upload and status behavior

- Upload is validated for file existence/count/per-file size/total request size.
- `202 Accepted` is returned only after batch registration and queue acceptance.
- Queue saturation returns `503` with queue-unavailable error and a completed batch diagnostic.
- Unknown batch IDs return `404`.
- Retrieval supports both in-progress (`Processing`) and completed (`Completed`) states.

### Response safety

API responses include only metadata:

- Statuses
- Property paths
- Diagnostics

Responses do not expose raw payload bytes or original JSON values.

Representative response shape:

- Batch: `batchId`, `status`, `payloads[]`, `createdUtc`, `completedUtc`
- Payload: `payloadIndex`, `documents[]`, `diagnostics[]`
- Document: `documentIndex`, `type`, `status`, `propertyPaths[]`, `diagnostics[]`

## Configuration and resource limits

`UploadLimits`

- `MaxRequestBytes` (default 10 MiB)
- `MaxFileBytes` (default 2 MiB)
- `MaxFileCount` (default 32)

`Extraction`

- `MaxCandidateBytes` (default 1 MiB)
- `MaxNestingDepth` (default 64)
- `MaxPathsPerDocument` (default 4096)
- `MaxDocumentsPerPayload` (default 512)

`BackgroundProcessing`

- `QueueCapacity` (default 256)

## In-memory storage and restart behavior

Storage is in-memory only:

- Data is shared safely across requests within the process lifetime.
- Data is lost on service restart.
- No durability, replication, or cross-process consistency guarantees.

Automatic expiration/TTL cleanup is not part of the current implementation.

## Background processing, cancellation, and failure isolation

- Bounded queue prevents unbounded pending work.
- Queue rejection is deterministic and recorded as a batch-level diagnostic.
- Background worker catches per-batch failures and continues processing subsequent batches.
- Cancellation is propagated through upload, enqueue, and processing paths.
- Cancellation and queue rejection paths avoid leaving batches in `Processing`.
- Raw payload buffers are cleared once processing finishes or work is rejected/cancelled.

## Observability

Structured logging is emitted at ownership boundaries with context such as:

- `BatchId`
- `FailureCategory`
- Stable diagnostic code
- Processing duration (ms)

Sensitive content is excluded from logs (no payload bytes, no full JSON, no property values).

## Known limitations and deferred capabilities

- In-memory storage is non-durable and single-process.
- No authentication/authorization.
- No distributed queueing, retries, or broker integration.
- No persistence encryption-at-rest (no durable store exists yet).
- No per-tenant rate limiting or quota controls.
