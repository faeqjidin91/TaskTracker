# TaskTracker — project rules

## What this is
A senior-level take-home: a Task Tracker (Projects contain Tasks with a status).
Graded on: clean project separation, auth, external-integration abstraction,
error handling, and README quality. Keep the domain simple; keep the architecture clean.

## Stack (do not change)
- Multi-targeted: .NET 10 (primary) and .NET 8, built with SDK 10 (pinned in global.json)
- Nullable enabled, implicit usings. Code must compile on both targets (C# 12 features
  are the safe baseline; guard anything newer or TFM-specific with `#if NET10_0_OR_GREATER`)
- Target frameworks are set only in Directory.Build.props
- Package versions are set only in Directory.Packages.props, conditioned per TFM where needed
- EF Core matching each target (10.x / 8.x) + SQL Server (local: Docker `mcr.microsoft.com/mssql/server:2022-latest`)
- ASP.NET Core Web API with controllers + Swagger (Swashbuckle)
- ASP.NET Core Identity + JWT bearer auth
- Blazor WebAssembly (standalone) for the UI
- FluentValidation for request validation
- xUnit for tests

## Solution layout and allowed references
src/TaskTracker.Domain          -> references nothing, no NuGet packages
src/TaskTracker.Contracts       -> references nothing (DTOs only)
src/TaskTracker.Application     -> Domain, Contracts
src/TaskTracker.Infrastructure  -> Application, Domain
src/TaskTracker.Api             -> Application, Infrastructure, Contracts
src/TaskTracker.Web             -> Contracts ONLY
tests/TaskTracker.Application.Tests -> Application, Domain

Never add a reference that breaks this graph.

## Conventions
- Entity name is `TaskItem` (never `Task`, it clashes with System.Threading.Tasks.Task).
- Status enum is `TaskItemStatus` (never `TaskStatus`, it clashes with System.Threading.Tasks.TaskStatus).
- Controllers are thin: no DbContext in controllers; they call Application services.
- All EF calls async with CancellationToken.
- DTOs are `record` types in Contracts. Never return entities from the API.
- Errors return RFC 7807 ProblemDetails. Status codes: 200, 201 + Location, 204, 400, 401, 403, 404, 409.
- Project and task GET endpoints are anonymous. User endpoints and `/api/auth/me` always require auth.
- POST/PUT/PATCH/DELETE require a named policy from `Policies`, except login and change-password.
- Roles are global, one per user: Admin (everything), Member (manage tasks), Viewer (read only).
- ASP.NET Core Identity stays in Infrastructure, behind `IUserDirectory`.
- Program.cs stays thin: use extension methods (AddApplication, AddInfrastructure, etc.).
- No secrets committed. Dev JWT key goes in `dotnet user-secrets`.
- Notification failures must never fail the main request: catch, log, continue.

## Working rules
- Work only on the phase I give you. Don't start later phases.
- After each phase: run `dotnet build` and fix all errors and warnings you introduced.
- At the end of each phase, list the files you created/changed and how to verify.
- If something in the brief is ambiguous, ask me instead of guessing.
