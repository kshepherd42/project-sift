# ProjectSift

Minimal full-stack monorepo with .NET 10 (C#, ASP.NET Core Web API), basic DDD/Clean Architecture layering, and a React + TypeScript frontend.

## Stack
- Backend: .NET 10, ASP.NET Core Web API, xUnit
- Frontend: React, TypeScript, Vite, Vitest, React Testing Library
- Startup: `concurrently` from repository root

## Structure
```text
ProjectSift.sln
package.json
src/
  ProjectSift.Domain/
  ProjectSift.Application/
  ProjectSift.Service/
tests/
  ProjectSift.Domain.Tests/
  ProjectSift.Application.Tests/
  ProjectSift.Service.Tests/
web/
```

## Layer responsibilities
- Domain: Core domain types/rules, no external project dependencies.
- Application: Use-case level abstractions and logic; references Domain only.
- Service: ASP.NET Core host, HTTP endpoints, DI, CORS; references Application and Domain.

## Prerequisites
- .NET SDK 10.0.100+
- Node.js and npm

## One-command setup and run
From repository root, after cloning:

- `npm run setup:dev`

This restores .NET packages, installs root/frontend npm dependencies, and starts backend + frontend together.

If you want to precreate frontend env config, copy `web/.env.example` to `web/.env`.

## Start both apps
From repository root:

- `npm run dev`

URLs:
- Frontend: http://localhost:5173
- Backend health endpoint: http://localhost:5050/api/health

## Run tests
- Backend tests: `dotnet test ProjectSift.sln`
- Frontend tests: `npm run test --prefix web`

## Frontend build
- `npm run build --prefix web`