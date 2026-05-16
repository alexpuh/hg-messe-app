# Copilot Instructions

## Repository Overview

This is a trade-show barcode-scanning application with three sub-projects:

- **`client/`** — Angular 21 SPA (TypeScript, NgRx Signal Store, PrimeNG, TailwindCSS v4)
- **`server/messe-server/`** — ASP.NET Core 9 REST API + SignalR hub, SQLite via EF Core
- **`server/messe-app/`** — WPF desktop host (Windows only) that launches `messe-server` and embeds the web UI in WebView2

## Build & Run Commands

### Client (Angular)
```bash
cd client
npm start           # dev server at http://localhost:4200 (proxies /api and /hubs → localhost:5227)
npm run build       # production build
npm test            # run unit tests (vitest via @angular/build:unit-test)
```

### Server (ASP.NET Core)
```bash
cd server/messe-server
dotnet run          # API at http://localhost:5227, Swagger UI at /swagger
dotnet build -c Release
```

### Desktop App (WPF)
```bash
cd server/messe-app
dotnet run          # auto-starts messe-server and shows WebView2
```

### Regenerate OpenAPI client
Start the server, then:
```cmd
cd client/src/app/api/openapi
gen-backend.cmd     # requires server running at localhost:5227
```

## Architecture

### Data flow
The Angular client calls `service wrappers` → which delegate to `generated OpenAPI services` in `client/src/app/api/openapi/backend/` → over HTTP to the ASP.NET Core API. Real-time updates come via SignalR at `/hubs/notification`.

### State management
All shared state lives in NgRx Signal Stores under `client/src/app/store/`. Currently one store: `ScanSessionStore` (provided in root). The store initializes on app startup, loads data, and wires up SignalR listeners for live barcode scan and scanner-status events.

### Server-side data model (SQLite via EF Core)
`ArticleUnit` → scanned via EAN (unit or box). `DispatchSheet` contains `DispatchSheetRequiredUnit` entries. A `ScanSession` (with `SessionType` = `ProcessDispatchList` | `Inventory` and `Ort` = `Stand` | `Lager`) belongs to a `DispatchSheet` and has `ScannedArticle` entries, each with `BarcodeScan` history. Schema is created via `EnsureCreatedAsync` at startup (no migrations).

### Dev proxy
`client/proxy.conf.json` forwards `/api/*` and `/hubs/*` to `http://localhost:5227` during `ng serve`, so the Angular app and API can run together without CORS issues.

## Barcode Scanner Integration

The physical scanner connects over a serial port (COM, 9600 baud, 8N1). The flow is:

1. **`BarcodeScannerService`** (Singleton) — owns the `SerialPort`. Reads lines in a blocking loop inside `StartScan()`. After a successful scan it sends ACK (`0x06`) back to the device; on failure it sends BEL (`0x07`). After ~5 seconds of consecutive read timeouts it closes and reopens the port to detect device disconnects.
2. **`BarcodeScannerBackgroundService`** (hosted service) — wraps the singleton in a retry loop. On connection loss it waits 15 s before reconnecting. It creates a DI scope per event to call scoped services (`ScanSessionService`, `SignalNotificationService`).
3. **`SignalNotificationService`** — thin wrapper around `IHubContext<NotificationHub>` that fires `BarcodeScanned`, `BarcodeError`, or `ScannerStatusChanged` to all SignalR clients.
4. **Angular `SignalrService`** — connects to `/hubs/notification`, registers `on*` callbacks, and is started in `ScanSessionStore.onInit`. The store reloads articles on `BarcodeScanned` and reloads scanner status on `ScannerStatusChanged`.

When adding new real-time events: add a `Send*` method to `SignalNotificationService`, add an `on*` method to `SignalrService`, and wire it up in `ScanSessionStore` (or the relevant store).

## Excel Export Flow

`GET /api/ScanSessions/{id}/articles/excel` returns an `.xlsx` file.

- `ScanSessionExcelExportService.Generate(session, articles, showExpectation, title)` uses **ClosedXML** to build the workbook in a `MemoryStream`.
- The `showExpectation` flag is `true` when `sessionType == ProcessDispatchList` **or** `session.Ort == Lager` — this adds "Soll" (required) and "Fehlt" (missing = required − scanned) columns.
- The `title` parameter sets the worksheet header cell and tab name. Controller computes it: `ProcessDispatchList → "Beladung"`, `Inventory+Lager → "Bestandsaufnahme Lager"`, `Inventory+Stand → "Messestand"`.
- Rows are sorted by `ArticleNr` then `UnitWeight`.
- The controller injects `ScanSessionExcelExportService` via `[FromServices]` (not constructor) because it is only needed for this one action.

`GET /api/ScanSessions/combined/excel` returns the Messeabschluss combined export.

- `ScanSessionExcelExportService.GenerateCombined(articles)` generates the combined workbook.
- Worksheet tab and header cell are both titled **"Messeabschluss"**.
- Columns: Art.Nr., Artikel, Gewicht, EAN, Stand Ist, Lager Ist, Gesamt, Soll, Fehlt.

## WPF Desktop App (`server/messe-app/`)

- Namespace: `Herrmann.MesseApp.Windows` (distinct from the server's `Herrmann.MesseApp.Server`).
- The `.csproj` has a **ProjectReference** to `messe-server` — the two projects are built together from `server.sln`.
- At runtime the app looks for `messe-server.exe` at `{AppBase}/server/messe-server.exe` (relative to where `messe-app.exe` lives). The expected deployment layout is:
  ```
  messe-app.exe
  server/
    messe-server.exe
    messeapp.db
  ```
- The WPF window polls `http://localhost:5227` every 500 ms (up to 30 s) before loading the Angular SPA in the embedded WebView2.
- On `Window_Closing` the server process is killed with `Kill(entireProcessTree: true)`.
- This project is **Windows-only** (`net9.0-windows`, WPF + WebView2).

## Key Conventions

### Angular
- **Standalone components** — do not use NgModules. Do not set `standalone: true` explicitly (it is the default in Angular v20+).
- **Signals** — use `signal()`, `computed()`, `input()`, `output()` functions; do not use `@Input`/`@Output` decorators or `@HostBinding`/`@HostListener`.
- **`inject()`** — use the `inject()` function for dependency injection, not constructor injection.
- **`ChangeDetectionStrategy.OnPush`** — set on every component.
- **Native control flow** — use `@if`, `@for`, `@switch` in templates; not `*ngIf`, `*ngFor`, `*ngSwitch`.
- **No `ngClass` / `ngStyle`** — use `[class]` and `[style]` bindings instead.
- **No arrow functions in templates** — they are not supported.
- **Reactive forms** — prefer over template-driven forms.
- **`NgOptimizedImage`** — use for all static images (not inline base64).
- **AXE / WCAG AA** — all components must pass AXE checks and meet WCAG AA (focus management, color contrast, ARIA).

### API service layer
- Generated OpenAPI services (suffix `OpenApi`, file suffix `.openapi.service`) live in `client/src/app/api/openapi/backend/` — **do not edit these files**; regenerate with `gen-backend.cmd`.
- **Never inject OpenAPI services directly into components.** Wrap them in a hand-written service (e.g., `ScanSessionsService`, `DispatchSheetsService`) under `client/src/app/api/`.
- OpenAPI DTOs (`DtoXxx` types) may be used directly in components and stores.

### Server (C#)
- Namespace root: `Herrmann.MesseApp.Server`
- DTOs are prefixed with `Dto` (e.g., `DtoArticle`, `DtoScanSession`).
- Enums are serialized as strings via `JsonStringEnumConverter` (registered globally in `Program.cs`).
- Scoped services: `DispatchSheetService`, `ArticlesService`, `ScanSessionService`, `SignalNotificationService`, `ScanSessionExcelExportService`. Singleton: `BarcodeScannerService`. Background service: `BarcodeScannerBackgroundService`.
- Swagger UI is only enabled in Development (`app.Environment.IsDevelopment()`).
- Logging via Serilog (configured from `appsettings.json`).

### Language
- **Code comments, commit messages, documentation, and all repository text must be in English.**
- **UI labels remain in German** (as the application targets German-speaking users).
- The language used in chat or issue discussions does not affect this rule.

### Formatting
- Prettier (client): `printWidth: 100`, `singleQuote: true`, Angular parser for `.html` files.
- SCSS for component styles.
