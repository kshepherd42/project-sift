# Copilot instructions for ProjectSift

## Big picture
- This repo is a full-stack monorepo: .NET backend + React/Vite frontend.
- Backend layering is strict and intentional:
  - Domain (`src/ProjectSift.Domain`): domain types/rules only, no project references.
  - Application (`src/ProjectSift.Application`): use-case logic + abstractions; references Domain only.
  - Service (`src/ProjectSift.Service`): ASP.NET Core host, endpoints, DI, CORS; references Application + Domain.
- Keep dependencies inward only (`Service -> Application -> Domain`).
- Preserve backend top-level `Program.cs` style and frontend function component style.

## Developer workflows
- Use .NET SDK `10.0.100` (see `global.json`).
- One-command setup + run from repo root: `npm run setup:dev`.
- Run backend + frontend together: `npm run dev`.
- Backend tests: `dotnet test ProjectSift.sln` (or `npm run test:backend`).
- Frontend tests: `npm run test --prefix web` (or `npm run test:frontend`).
- Frontend build: `npm run build --prefix web` (or `npm run build:frontend`).

## Testing conventions to preserve
- Backend unit tests use xUnit in mirrored test projects under `tests/`.
- Service integration tests use `Microsoft.AspNetCore.Mvc.Testing` with `WebApplicationFactory<Program>`.
- Frontend tests use Vitest + Testing Library and validate user-visible behavior.
- In tests, prefer AutoFixture for generating fake data instead of hand-building repetitive objects.

## Change guidance for agents
- For new backend features, add domain model first, then application abstraction/implementation, then service endpoint + DI registration, then tests in matching layer.
- For new frontend API features, add/extend gateway functions before UI changes.
- Keep edits small and consistent with existing structure and naming patterns.
- If API response contracts change, update backend and frontend tests in the same change.

## Coding preferences for this repo
- In C# services/providers/classes, prefer primary constructors when they improve readability and fit the current style.
- Validate constructor and method arguments for null at boundaries (for example with `ArgumentNullException.ThrowIfNull(...)`).
- Avoid magic strings in backend code; introduce project-local constants classes as needed (for example endpoint routes, policy names, status text keys).
- For repeated validation rules, use a shared guard helper instead of duplicating inline checks (especially null/whitespace, non-negative index, and positive numeric checks).
- Prefer `Guard` APIs over manual `if (...) throw ...` validation when an equivalent guard already exists.
- In extraction/scanning code, keep diagnostic/error messages centralized in constants/message helpers rather than inline string literals in providers.
- Keep `using` directives clean: remove unused `using`s and keep remaining `using`s alphabetized with `System.*` first, then project/other namespaces.
- Prefer intent-revealing private helper methods to break up long methods and complex switch/branch logic.
- Favor descriptive method names over explanatory comments; keep comments only for non-obvious invariants or protocol constraints that names alone cannot communicate.
- Keep classes focused on a single responsibility. When a provider starts owning boundary scanning, result construction, formatting, and orchestration together, split those concerns into dedicated collaborators (for example scanners, collectors, and factories).
- Keep grouped property declarations compact; do not insert blank lines between consecutive properties.
- In tests, do not use `fixture.Create<string>()` for identifiers or labels. Use explicit meaningful constants/values instead.
- Organize C# files into clear subfolders by role (for example `Models`, `Enums`, `Abstractions`, `Providers`, `Constants`, `Validation`) instead of keeping many unrelated files at the project root.
- Put each C# enum in its own file under an `Enums` folder; avoid nested enums inside non-test classes.
- Align C# namespaces with folder structure (for example `ProjectSift.Domain.Models`, `ProjectSift.Application.Abstractions`, `ProjectSift.Service.Constants`) and update dependent `using` statements in the same change.
- When relocating files, preserve Git history visibility by performing explicit file moves (rename/move) rather than delete-and-recreate patterns where possible.
