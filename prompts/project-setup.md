Task: Create ProjectSift — Minimal Full-Stack Monorepo

Create a complete, working GitHub monorepo named ProjectSift in the current workspace.


1. Technology and scope
- .NET 10, C#, ASP.NET Core Web API.
- Basic DDD and Clean Architecture.
- xUnit for backend tests.
- React, TypeScript, Vite, and npm.
- Vitest and React Testing Library for frontend tests.

2. Repository structure

Create this structure:

- ProjectSift.sln
- package.json — root-level development scripts.
- .gitignore
- README.md — short setup and usage instructions.
- src/ProjectSift.Domain/
- src/ProjectSift.Application/
- src/ProjectSift.Service/
- tests/ProjectSift.Domain.Tests/
- tests/ProjectSift.Application.Tests/
- tests/ProjectSift.Service.Tests/
- web/ — React frontend.
- web/.env.example

Include all required project files, configuration, source code, and lockfiles.

Keep the structure simple. Do not create empty directories, placeholder classes, or files that serve no actual purpose.

3. Backend architecture

Create three production projects and one corresponding xUnit test project for each.

ProjectSift.Domain: innermost layer, intended for entities, value objects, business rules, and domain exceptions.

ProjectSift.Application: middle layer, intended for application use cases and application-level abstractions.

ProjectSift.Service: outermost layer, an executable ASP.NET Core Web API, owns HTTP endpoints, application startup, and dependency injection configuration.

Enforce these project references:

Domain → no project references.
Application → Domain.
Service → Application and Domain.

Keep the architecture ready for future business logic without inventing business entities or application services that the initial functionality does not need.

4. Implement the backend API

Implement this endpoint in ProjectSift.Service:

GET /api/health

It must:

Return HTTP 200 OK when the API is running.
Return a small JSON body, such as { "status": "Healthy" }.
Require no database, authentication, or external service.
Be accessible through a normal HTTP request.

5. React frontend

Create a React and TypeScript application using Vite.

Organize its source code as follows:

web/src/gateway/ — centralized API calls and API response types.
web/src/features/health/ — health feature logic and UI.
web/src/components/ — reusable components only when needed.
web/src/App.tsx — main page.
web/src/main.tsx — application entry point.

Keep the frontend small. Do not add state-management libraries or unnecessary component layers.

Required behavior

When the user opens the frontend:

Display the ProjectSift title.
Call the backend's GET /api/health endpoint.
Display the actual HTTP response status code, such as 200.
Display the JSON response from the backend.
Display a loading state while the request is in progress.
Display a useful error message if the request fails.
Provide a retry button.

The frontend must make a real HTTP request. Do not hardcode a successful status code or simulate a successful backend response in the actual application.

API gateway and configuration

All frontend API calls must go through web/src/gateway/. The health feature must not make direct HTTP requests itself.

Use the browser's built-in fetch API. Centralize the API base URL and request handling so additional API calls can be added later without duplicating configuration.

Configure the base URL through VITE_API_BASE_URL. Include web/.env.example and ensure the configured URL matches the backend's actual listening port.

Configure ASP.NET Core CORS to permit the local Vite development origin. Do not allow every origin indiscriminately.

6. Backend tests

Use xUnit and create:

ProjectSift.Domain.Tests
ProjectSift.Application.Tests
ProjectSift.Service.Tests

Include all three test projects in the solution.

Add an integration test to ProjectSift.Service.Tests using Microsoft.AspNetCore.Mvc.Testing or an equivalent lightweight test-host approach. Verify that GET /api/health returns HTTP 200 and the expected JSON response.

Keep the Domain and Application test projects valid and buildable. Add tests only for meaningful behavior that actually exists; do not invent domain logic to increase test coverage.

7. Frontend tests

Use Vitest and React Testing Library.

Include tests for:

Successful API responses: display the actual returned status code.
Failed requests: display an appropriate error state.
Retry: trigger another API request.

Mock HTTP requests in frontend tests. Tests must not require a running backend.

Configure the necessary npm scripts and testing environment. Do not add linting or end-to-end browser-testing frameworks.

8. One-command startup

Create a root package.json with a dev script that uses concurrently to start both the backend and frontend.

Also include a root bootstrap script named `setup:dev` that runs initial dependency installation and then starts the app stack:

- `dotnet restore ProjectSift.sln`
- `npm ci`
- `npm ci --prefix web`
- `npm run dev`

The `setup:dev` script may chain these commands in one script so a fresh clone can be set up and run with one command.

The developer must be able to run this command from the repository root in the VS Code integrated terminal:

npm run dev

It must work on Windows, macOS, and Linux, assuming Node.js/npm and the .NET 10 SDK are installed.

Requirements:

Start the .NET API and Vite development server together.
Use consistent ports and API configuration.
Print the frontend and backend URLs.
Allow Ctrl+C to stop both processes.
Report startup failures clearly.
Do not require Bash, PowerShell, Docker, or platform-specific scripts.

Keep initial dependency installation separate from normal startup. Document the installation commands in the README.

Use concurrently rather than implementing custom process management.

9. Repository configuration

Create a root .gitignore that excludes:

.NET bin/ and obj/ directories.
node_modules/.
Frontend build output.
Test results and coverage output.
IDE and operating-system artifacts.
Local environment files and logs.

Keep source code, project files, solution files, lockfiles, documentation, and .env.example tracked.

Do not add CI, linting, formatting frameworks, or other optional tooling.

10. README

Keep the root README.md concise. Include only:

Project purpose and technology stack.
A compact directory structure.
The responsibilities of Domain, Application, and Service.
Required prerequisites.
Initial dependency installation.
How to start everything using npm run dev.
The frontend URL and backend health endpoint.
How to run backend and frontend tests.

Ensure every command, path, port, and URL matches the actual implementation.

11. Verification

After implementation, perform one consolidated verification pass:

Restore and build ProjectSift.sln.
Run all backend tests.
Run npm ci at the repository root and inside web/.
Run frontend tests and the production build.
Start both applications using npm run dev.
Send a real HTTP request to /api/health.
Verify that the frontend can reach the API and display its actual response status.
Verify that Ctrl+C terminates both servers.

Fix failures with the smallest targeted changes. Do not repeat successful checks without a concrete reason. If a check cannot be performed in the available environment, report it accurately instead of claiming success.