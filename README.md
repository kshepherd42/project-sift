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
  - Returns `400` for invalid uploads with aggregated validation details.
  - Returns `503` when queue capacity is full.
- `GET /v1/batches/{batchId}`
  - Returns `200` with `Processing` or `Completed` status.
  - Returns `404` for unknown batch IDs.
- `GET /api/health`

Responses never include raw payload bytes or original JSON values.
Responses include both index and optional name fields (`payloadIndex` + `payloadName`, `documentIndex` + `documentName`) for stable ordering plus readable identifiers.

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
- Frontend build: `npm run build --prefix web`

## Technical design reference

See [docs/backend-technical-design.md](docs/backend-technical-design.md) for the detailed backend design and hardening behavior.