# ProjectSift

ProjectSift is a full-stack monorepo with a .NET 10 backend and a React/Vite frontend. The backend is intentionally layered and contract-first to keep parsing, processing, and HTTP concerns isolated.

## Stack

- Backend: .NET 10, ASP.NET Core Web API, xUnit
- Frontend: React, TypeScript, Vite, Vitest, React Testing Library
- Root orchestration: npm scripts with `concurrently`

## Repository structure

```text
ProjectSift.sln
package.json
src/
  ProjectSift.Domain/
  ProjectSift.Application/
  ProjectSift.Infrastructure/
  ProjectSift.Service/
tests/
  ProjectSift.Domain.Tests/
  ProjectSift.Application.Tests/
  ProjectSift.Infrastructure.Tests/
  ProjectSift.Service.Tests/
web/
docs/
```

## Backend architecture

Dependency direction is strict and inward:

- `ProjectSift.Domain` (no project references)
- `ProjectSift.Application` -> Domain
- `ProjectSift.Infrastructure` -> Domain
- `ProjectSift.Service` -> Application + Domain + Infrastructure

### Domain

- Defines result models (`BatchResult`, `PayloadResult`, `DocumentResult`), diagnostics, enums, guards, and exceptions.
- Owns infrastructure contract `IBatchStore`.

### Application

- Scans binary/noisy payloads for JSON object/array candidates.
- Performs conservative boundary recovery only when unambiguous.
- Parses and traverses JSON via `System.Text.Json`.
- Collects deterministic, deduplicated property paths.
- Coordinates per-payload extraction and batch-level aggregation.

### Infrastructure

- Implements `IBatchStore` using in-memory shared state.
- Uses optimistic concurrency with ETag checks.
- Provides thread-safe create/read/update semantics.

### Service

- Exposes upload and retrieval HTTP endpoints.
- Validates upload limits before processing.
- Registers batch and enqueues work to a bounded in-process queue.
- Runs background batch processing with per-batch failure isolation.
- Maps API exceptions to stable HTTP error responses.

## API overview

- `POST /v1/batches` (multipart form field `files`)
  - Returns `202 Accepted` only after batch registration and queue acceptance.
  - Response contains `batchId` and `resourceUrl`; `Location` also points to the retrieval resource.
  - Returns `400` for invalid uploads with aggregated validation details.
  - Returns `503` when queue capacity is full; the error includes the batch ID when a rejection record was created.
- `GET /v1/batches/{batchId}`
  - Returns `200` with `Processing` or `Completed` status and nested payload/document results.
  - Returns `404` for unknown batch IDs.
- `GET /api/health`

Responses never include raw payload bytes or original JSON values.
Responses include both index and optional name fields (`payloadIndex` + `payloadName`, `documentIndex` + `documentName`) for stable ordering plus readable identifiers.
Batch status is `Processing` or terminal `Completed`. Document status is `Successful`, `Recovered`, or `Failed`. Queue rejection is represented by an error response and retrievable completed diagnostic record, not by a new batch status.

## Resource limits and safety controls

Configured in [src/ProjectSift.Service/appsettings.json](src/ProjectSift.Service/appsettings.json):

- `UploadLimits`
  - `MaxRequestBytes` (default 10 MiB)
  - `MaxFileBytes` (default 2 MiB)
  - `MaxFileCount` (default 32)
- `Extraction`
  - `MaxCandidateBytes` (default 1 MiB)
  - `MaxNestingDepth` (default 64)
  - `MaxPathsPerDocument` (default 4096)
  - `MaxDocumentsPerPayload` (default 512)
- `BackgroundProcessing`
  - `QueueCapacity` (default 256)

Additional hardening:

- cancellation propagation through upload, enqueue, scan, and processing
- payload buffer clearing after completion/rejection/cancellation
- deterministic diagnostics for limit and parsing failures

## Storage behavior

- In-memory only (non-durable)
- Records are lost on service restart
- No automatic TTL expiration in current behavior

## Frontend workflows and limits

- Upload accepts one or more files as a single batch. Client validation mirrors the backend defaults: 32 files, 2 MiB per file, and 10 MiB combined request size. Server validation remains authoritative. If backend limits are changed, update `VITE_UPLOAD_MAX_FILE_COUNT`, `VITE_UPLOAD_MAX_FILE_BYTES`, and `VITE_UPLOAD_MAX_REQUEST_BYTES` to match.
- Local development defaults the API origin to `http://localhost:5050`. Production builds require `VITE_API_BASE_URL`; a missing or invalid value is reported in the UI instead of silently requesting localhost. `web/.env.example` shows the development value.
- The dashboard's Recent activity is browser-local: it keeps up to eight recently opened batch IDs and timestamps in `localStorage`. It is not server history, is not shared between browsers, and entries can become stale after backend restart because batch storage is in-memory.
- The backend has no list/history endpoint, so the dashboard cannot discover all batches or provide server-wide history search, filters, or pagination. Direct lookup by a known batch ID is available.
- Batch details poll verified `Processing` batches every 3 seconds, time out an individual request after 10 seconds, and retry transient failures after 2, 5, and 10 seconds before pausing automatic refresh. Returning focus/visibility triggers a refresh. Manual refresh remains available, and the last successful response is retained with a stale-data warning after refresh failures. Unknown statuses are shown neutrally and are not polled.
- Batch-details search operates only on the currently loaded batch. It matches property paths, payload/document names and statuses, and diagnostic text; matching is case-insensitive substring search. It does not search unloaded batches.
- Path display supports occurrence and unique-value modes. Occurrences preserve repeated values and source order; unique mode keeps the first occurrence of each exact path value. Copy actions use the current display mode. JSON export uses the selected path mode and retains the first occurrence's context for unique paths. CSV export is always one row per matching occurrence, even while the display is in unique mode.
- CSV columns are `batchId`, `payloadIndex`, `payloadName`, `documentIndex`, `documentName`, `documentStatus`, and `propertyPath`, in that order. Every field is quoted, embedded quotes are doubled, rows use CRLF, and the browser emits UTF-8 without a BOM. JSON export contains `batchId`, `generatedAtUtc`, `scope`, `query`, and grouped payload/document path data; it does not include raw source values.
- The API response currently has payload- and document-level diagnostics, but no batch-level diagnostics or history collection. Frontend response-normalization warnings are surfaced separately from backend diagnostics.

## Observability

Structured logs are emitted around submission and background processing boundaries with contextual fields such as:

- `BatchId`
- failure category
- diagnostic code
- processing duration

Sensitive payload contents are not logged.

## Prerequisites

- .NET SDK `10.0.100`+
- Node.js + npm

## Setup and run

From repository root:

- `npm run setup:dev`

This restores .NET packages, installs npm dependencies, and starts backend + frontend.

If needed, copy `web/.env.example` to `web/.env` first.

### Start both apps

- `npm run dev`

Default URLs:

- Frontend: http://localhost:5173
- Backend health: http://localhost:5050/api/health

## Build and test

- Backend tests: `dotnet test ProjectSift.sln`
- Frontend tests: `npm run test --prefix web`
- Frontend production build and TypeScript project checks: `npm run build --prefix web`

There are no dedicated lint or format scripts configured in the current package manifests. Frontend unit tests use mocked API responses; backend service integration tests use `WebApplicationFactory` and in-process hosting. For real local API verification, run `npm run dev` and use the safe `.bin` examples documented in [sample payloads/README.md](sample%20payloads/README.md).

## Technical design reference

See [docs/backend-technical-design.md](docs/backend-technical-design.md) for the detailed backend design and hardening behavior.