# LeaveMate

Corporate leave management system. ASP.NET Core Razor Pages front end,
C# controller/service backend, EF Core over SQLite (dev) / SQL Server
(production), per the PROGRAMMING II semester project proposal.

## Running locally

```bash
dotnet restore
dotnet run --project LeaveMate
```

The app seeds a demo dataset on first run (an HR Administrator, a
Supervisor, and two direct reports) and serves both the UI and the
JSON API from the same process:

- UI: http://localhost:5000
- API docs (Swagger, dev only): http://localhost:5000/swagger

To target SQL Server instead of the bundled SQLite file, set
`"UseSqlite": false` in `appsettings.json` and update
`ConnectionStrings:DefaultConnection`.

## Running the tests

```bash
dotnet test
```

## Running in Docker

```bash
docker compose up --build
```

## Solution layout and team ownership

This mirrors the 12-member role allocation from the project proposal:

| Area | Files | Track |
|---|---|---|
| Architecture / DI wiring | `Program.cs` | Lead Architect & PM |
| Compliance criteria & tests | `LeaveMate.Tests/*` | Product Owner & QA Engineer |
| Shell layout, theme | `Pages/Shared/_Layout.cshtml`, `wwwroot/css/site.css` | Lead UI Designer |
| Request form bindings | `Pages/Requests/*` | UI Developer |
| Coverage calendar | `Pages/Calendar.cshtml`, `wwwroot/js/calendar.js` | UI Developer |
| Validation engine, controllers | `Services/Validation/*`, `Controllers/LeaveRequestsController.cs` | Lead Backend Developer (C#) |
| HTTP routing & exception logic | `Controllers/EmployeesController.cs`, `Middleware/ExceptionHandlingMiddleware.cs` | Backend Developer |
| Async handlers & logging | `Services/Integration/CoverageRefreshService.cs`, `Middleware/RequestLoggingMiddleware.cs` | Backend Developer |
| Table/key design, seed data | `Data/ApplicationDbContext.cs`, `Data/DbSeeder.cs` | Database Administrator |
| EF Core models & connections | `Models/*` | Data Access Engineer |
| Git/deployment | `Dockerfile`, `docker-compose.yml`, this README | DevOps & Integration Specialist |
| UI ↔ controller integration | `Services/Integration/LeaveMateApiClient.cs` | Systems Integrator |

## Git branching strategy

To let 12 people work in parallel without conflicts:

- `main` — always deployable.
- `develop` — integration branch; feature branches merge here first.
- `feature/<track>-<short-desc>` — one branch per unit of work, e.g.
  `feature/backend-validation-engine`, `feature/ui-calendar`.
- Open a PR into `develop` for review before merging; `develop` merges
  into `main` at sprint milestones.
- Keep `Models/`, `DTOs/`, and `Services/Validation/` changes small and
  reviewed quickly — most other tracks depend on their shapes staying
  stable.

## Key functionality implemented

1. **Automated multi-tier workflow** — `LeaveWorkflowService` routes a
   request Employee → Supervisor → HR, rejecting out-of-turn actions.
2. **Context-aware validation engine** — `LeaveValidationService`
   rejects weekend starts, over-threshold durations, overlapping
   requests, and insufficient balance.
3. **Real-time coverage matrix** — `CoverageRefreshService` recomputes
   a 60-day coverage snapshot in the background every 30 seconds;
   `Pages/Calendar.cshtml` polls it for a live grid.
