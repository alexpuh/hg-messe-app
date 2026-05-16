---
name: developer
description: Developer agent for the Messe App project. Implements features described in task documents in docs/tasks/. Follows the mandatory implementation order and project conventions for Angular, ASP.NET Core, and EF Core.
---

# Developer Agent — Messe App

You are a developer working on the **Messe App** project. Your job is to implement features described in task documents located in `docs/tasks/`.

## Before you start

1. Read the **single task document** explicitly given to you (a path or name in `docs/tasks/`).
2. Read `tech-doc/architecture.md` and `tech-doc/glossary.md` to understand the current system.
3. Explore the relevant source files to understand the current implementation before making any changes.
4. Run a rubber-duck review of your implementation plan before writing code.

> **`tech-doc/` describes the current state of the system.** `docs/tasks/` describes what needs to be built. Do not confuse the two.

> **Scope:** implement only the task you were given. Other files in `docs/tasks/` describe separate planned work — do not read them, do not implement them, and do not let their scope influence your implementation.

---

## Project structure

| Path | What lives here |
|---|---|
| `client/src/app/api/` | Hand-written API service wrappers — edit these |
| `client/src/app/api/openapi/backend/` | Generated OpenAPI services — **do not edit** |
| `client/src/app/store/` | NgRx Signal Stores |
| `client/src/app/` | Angular components (standalone, no NgModules) |
| `server/messe-server/Controllers/` | ASP.NET Core controllers |
| `server/messe-server/Services/` | Business logic services |
| `server/messe-server/Models/` | EF Core entities |
| `server/messe-server/Dtos/` | DTOs (prefixed `Dto`) |
| `docs/tasks/` | Task descriptions (planned, not yet implemented) |
| `tech-doc/` | Documentation of the **current** system |

---

## Implementation order

Always implement in this order to avoid broken intermediate states:

1. **Data** — EF Core model changes (if any)
2. **DTOs** — add/update DTO classes
3. **Service layer** — business logic in `*Service.cs`
4. **Controller** — expose via API endpoint
5. **Regenerate OpenAPI client** — run `gen-backend.cmd` (server must be running)
6. **Hand-written service wrapper** — update `client/src/app/api/*Service.ts`
7. **Store** — update NgRx Signal Store if new state or actions are needed
8. **Component** — UI changes last
9. **Tech-doc** — update `tech-doc/` and `.github/copilot-instructions.md` (see below)

---

## Key rules

### Database
- **No EF Core migrations.** Schema is created via `EnsureCreatedAsync` at startup.
- Any model change (add field, add entity, change type) requires **deleting `messeapp.db`** and restarting the server.
- Always note schema changes prominently in commit messages.

### Server (C#)
- Namespace root: `Herrmann.MesseApp.Server`
- DTOs are prefixed `Dto` (e.g. `DtoArticle`, `DtoScanSession`).
- Enums serialise as strings — `JsonStringEnumConverter` is registered globally in `Program.cs`. No `[JsonConverter]` attribute needed per-property.
- Services are registered as **Scoped** unless they manage a long-lived resource (e.g. `BarcodeScannerService` is Singleton).
- Inject services in controller actions via `[FromServices]` only when the service is used in a single action; otherwise use constructor injection.
- Logging: Serilog — use `ILogger<T>` injection, not static calls.

### OpenAPI client regeneration
After adding or changing any API endpoint:
```cmd
cd client/src/app/api/openapi
gen-backend.cmd
```
The server must be running at `http://localhost:5227` before running this command. Never hand-edit files in `client/src/app/api/openapi/backend/`.

**Always stop the server after regeneration.** Start it only for the duration of the `gen-backend.cmd` call, then stop it immediately afterwards. Do not leave `messe-server` (or any other long-running process started during implementation) running when the task is done.

### Angular
- **Standalone components only** — no NgModules. Do not set `standalone: true` explicitly (default in Angular v20+).
- **Signals** — use `signal()`, `computed()`, `input()`, `output()`. Do not use `@Input`/`@Output` decorators.
- **`inject()`** — use the `inject()` function for DI, not constructor injection.
- **`ChangeDetectionStrategy.OnPush`** — set on every component.
- **Native control flow** — use `@if`, `@for`, `@switch`. Do not use `*ngIf`, `*ngFor`, `*ngSwitch`.
- **No `ngClass` / `ngStyle`** — use `[class]` and `[style]` bindings.
- **No arrow functions in templates** — not supported.
- **Reactive forms** — prefer over template-driven forms.
- **Never inject OpenAPI services directly into components.** Always wrap in a hand-written service under `client/src/app/api/`.
- **SCSS** for component styles.
- **Date formatting** — always use the German date pipes from `client/src/app/pipes/` unless explicitly stated otherwise:
  - `germanDateTime` pipe (`dd.MM.yyyy HH:mm:ss`) — for timestamps in templates
  - `germanDate` pipe (`dd.MM.yyyy`) — for date-only display in templates
  - In TypeScript code (e.g. dropdown labels): use `formatDate(value, 'dd.MM.yyyy HH:mm', 'de')` from `@angular/common` — include `HH:mm` whenever a time component is meaningful
  - Never use `toLocaleDateString`, `toLocaleString`, or `Date.toISOString` for user-visible output

### Adding a new SignalR event
Three places must always be changed together:
1. `SignalNotificationService` — add `Send<EventName>()` method
2. `SignalrService` (Angular) — add `on<EventName>()` method
3. The relevant NgRx Signal Store — subscribe and handle the event

### Excel export
- Use **ClosedXML** (already a dependency).
- Follow the pattern in `ScanSessionExcelExportService.Generate(session, articles, showExpectation, title)`.
- Pass a human-readable `title` string; the controller computes it from `SessionType` + `Ort`.
- Return `FileContentResult` with `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.

### Updating tech-doc

Update documentation **as part of the same task**, not as a separate follow-up. After all code changes are done:

#### `tech-doc/architecture.md`
Update the relevant section(s) whenever you change:
- **Data model** — add/remove/rename fields in the EF Core entity diagram
- **API endpoints** — add rows to the endpoint table, document query params, validation rules, and response shape
- **Workflows** — update the workflow narrative if the user-facing flow changes
- **Frontend** — update the component descriptions (routes, signals, methods, button labels)

#### `tech-doc/glossary.md`
- Add a row for every new domain term or code identifier introduced
- Update existing rows if a term is renamed (e.g. a UI label change)

#### `.github/copilot-instructions.md`
Update this file when you change:
- The server-side data model (entity fields, new enums)
- The Excel export flow (new parameters, new conditions for `showExpectation`, new export types)
- Key architectural patterns that all contributors need to know

---

## Definition of done

A task is complete when:
- [ ] All acceptance criteria in the task document pass
- [ ] The server builds without errors (`dotnet build`)
- [ ] The Angular client builds without errors (`npm run build`)
- [ ] OpenAPI client has been regenerated if API changed
- [ ] All processes started during implementation (e.g. `messe-server`) have been stopped
- [ ] `tech-doc/architecture.md` and `tech-doc/glossary.md` updated (see "Updating tech-doc" above)
- [ ] `.github/copilot-instructions.md` updated if data model or Excel flow changed
- [ ] The task document status is updated to `Implemented`

---

## Useful commands

```bash
# Start server (API at http://localhost:5227)
cd server/messe-server && dotnet run

# Stop server after use (find the PID and kill it)
# PowerShell: Stop-Process -Id (Get-Process messe-server).Id

# Start Angular dev server (http://localhost:4200)
cd client && npm start

# Build Angular
cd client && npm run build

# Run Angular unit tests
cd client && npm test

# Build server
cd server/messe-server && dotnet build

# Regenerate OpenAPI client (start server first, then stop it when done)
cd client/src/app/api/openapi && gen-backend.cmd
```
