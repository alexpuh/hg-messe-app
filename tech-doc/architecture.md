# Messe App

Trade-show barcode-scanning application for loading and inventory workflows. Consists of an Angular SPA, an ASP.NET Core 9 REST API with SignalR, and an optional WPF desktop host.

> **Single-user, single-machine application.** The app runs on one computer operated by one person at a time. Multi-user concurrency, authentication, and access control are explicitly out of scope. No concurrency handling (optimistic locking, conflict resolution, etc.) is required.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Data Model](#data-model)
3. [Workflows](#workflows)
   - [Beladeliste (Beladung)](#workflow-beladeliste--beladung)
   - [Bestandsaufnahme (Inventory)](#workflow-bestandsaufnahme)
4. [API Reference](#api-reference)
5. [Frontend](#frontend)
6. [WPF Desktop Integration](#wpf-desktop-integration)
7. [Deployment](#deployment)

See also: [Glossary (German / English)](./glossary.md)

---

## Architecture Overview

```
┌──────────────────────────────────────────┐
│  Angular SPA  (client/)                  │
│  NgRx Signal Store ← SignalR             │
│  Components: ScanSession, Config         │
└────────────┬─────────────────────────────┘
             │ HTTP /api/*   WebSocket /hubs/notification
             ▼
┌──────────────────────────────────────────┐
│  ASP.NET Core 9  (server/messe-server/)  │
│  Controllers → Services → EF Core       │
│  SQLite: messeapp.db                     │
│  SignalR: NotificationHub                │
│  SerialPort: BarcodeScannerService       │
└──────────────────────────────────────────┘
             ▲
             │  Hosts & embeds in WebView2
┌────────────┴─────────────────────────────┐
│  WPF App  (server/messe-app/)            │
│  Starts messe-server.exe, polls ready    │
└──────────────────────────────────────────┘
```

### Sub-projects

| Path | Type | Purpose |
|---|---|---|
| `client/` | Angular 21 SPA | User interface |
| `server/messe-server/` | ASP.NET Core 9 | REST API + SignalR + serial scanner |
| `server/messe-app/` | WPF .NET 9 (Windows) | Desktop host |

### Port assignments

| Port | Service |
|---|---|
| 4200 | Angular dev server (`npm start`) |
| 5227 | ASP.NET Core API (dev + production) |

---

## Data Model

```
ArticleUnit (pk: UnitId — externally assigned, not auto-increment)
  UnitId        int
  ArticleId     int
  ArtNr         string
  DisplayName   string
  Weight        int (grams)
  EanUnit       string?    ← barcode for a single unit
  EanBox        string?    ← barcode for a full box
  PackagesInBox int
  IsArticleDisabled bool
  IsUnitDisabled    bool

DispatchSheet (pk: Id auto-increment)
  Id        int
  Name      string
  CreatedAt DateTime
  └── RequiredUnits → DispatchSheetRequiredUnit[]  (CASCADE delete)

DispatchSheetRequiredUnit (unique: DispatchSheetId + UnitId)
  DispatchSheetId  int → DispatchSheet
  UnitId           int → ArticleUnit
  RequiredCount    int

ScanSession (pk: Id auto-increment)
  Id              int
  SessionType     enum  ProcessDispatchList | Inventory
  DispatchSheetId int? → DispatchSheet  (SET NULL on delete)
  StartedAt       DateTime
  UpdatedAt       DateTime   ← timestamp of most recent scan
  └── ScannedArticles → ScannedArticle[]  (CASCADE delete)

ScannedArticle (per unit per session)
  Id            int
  ScanSessionId int → ScanSession
  UnitId        int → ArticleUnit
  QuantityUnits int   ← incremented on every scan
  UpdatedAt     DateTime
  └── BarcodeScans → BarcodeScan[]  (CASCADE delete)

BarcodeScan (audit log of every scan event)
  Id          int
  StockItemId int → ScannedArticle
  Ean         string
  ScannedAt   DateTime
```

**Key rule:** `ArticleUnit.UnitId` is assigned externally (imported from JSON) and is not auto-incremented. The database is created from scratch via `EnsureCreatedAsync` at startup — there are no EF Core migrations.

---

## Workflows

### Workflow: Beladeliste (Beladung)

Used to verify the loading of goods against a pre-defined list.

#### Preparation (Config screen `/config`)

1. Open the **Konfiguration** screen.
2. Select or create a **Beladeliste** (dispatch sheet).
3. For each article unit that must be loaded, click its row and enter a **Sollbestand** (required count). Save with ✓.
4. Optionally upload a fresh article catalogue (JSON) using **Artikelliste** upload.

#### Execution (Scan screen `/scan-session`)

1. Click **Neue Beladung starten**.
2. Select the target Beladeliste from the dropdown. Click **Starten**.
   - Server creates a `ScanSession` with `SessionType = ProcessDispatchList` and the chosen `DispatchSheetId`.
3. The scanner operator scans barcodes. Each scan:
   - Hits the physical scanner (SerialPort, COM, 9600 baud)
   - `BarcodeScannerService` reads the line, looks up the EAN in `ArticlesService`
   - `ScanSessionService.AddBarcodeAsync` increments `ScannedArticle.QuantityUnits` and appends a `BarcodeScan` audit record
   - On success → ACK (0x06) sent to device; on failure → BEL (0x07)
   - `SignalNotificationService.SendBarcodeScanned` fires to all SignalR clients
   - Angular store receives `BarcodeScanned` and reloads the article list
4. The article list shows every scanned unit with **Ist** (actual count) and **Soll** (required count).
   - Unscanned units that have a required count are also shown with `Count = 0`.
5. Click **Excel exportieren** to download `{BeladelistenName}_{date}.xlsx`.
   - Excel includes **Bestand** (Ist), **Soll**, and **Fehlt** (Soll − Ist) columns.
   - Only `Fehlt` cells where `Ist < Soll` are filled; otherwise the cell is blank.

#### Article list on the server (`GetScanSessionArticlesAsync`)

The endpoint combines:
- All `ScannedArticle` rows for the session (with their `RequiredCount` from the dispatch sheet)
- All `DispatchSheetRequiredUnit` entries with `RequiredCount > 0` that were not yet scanned (so missing articles appear with `Count = 0`)

---

### Workflow: Bestandsaufnahme

Used for a free-form stock count without a predefined list.

#### Execution

1. Click **Bestandsaufnahme starten**. No dispatch sheet is needed. Click **Starten**.
   - Server creates a `ScanSession` with `SessionType = Inventory` and `DispatchSheetId = null`.
2. Scan barcodes. Same serial-port / SignalR flow as above.
3. The article list shows scanned articles with only the **Ist** column (no Soll, no Fehlt).
4. Click **Excel exportieren** to download the result.
   - Excel is a simple count sheet (no Soll / Fehlt columns).

#### Difference from Beladung

| | Beladeliste | Bestandsaufnahme |
|---|---|---|
| `SessionType` | `ProcessDispatchList` | `Inventory` |
| `DispatchSheetId` | required | `null` |
| Required counts shown | yes (Soll column) | no |
| Missing articles in list | yes (`Count = 0`) | no |
| Excel Soll/Fehlt columns | yes | no |

---

## API Reference

Base URL: `http://localhost:5227/api`

All responses use JSON. Enums serialise as strings (`ProcessDispatchList`, `Inventory`). Swagger UI available at `/swagger` in Development mode.

---

### Articles

#### `POST /api/Articles/upload-articles`
Upload a new article catalogue (replaces all existing `ArticleUnits`).

- Content-Type: `multipart/form-data`
- Field: `file` (JSON file)

**JSON format:**
```json
[
  {
    "id": 1,
    "artNr": "12345",
    "displayName": "Muster Artikel",
    "isDisabled": false,
    "units": [
      { "id": 101, "articleId": 1, "weight": 500, "ean": "4000001234567", "packagesInBox": 6, "isDisabled": false }
    ]
  }
]
```
Response: `{ "importedCount": 42, "message": "42 Artikel erfolgreich importiert" }`

#### `GET /api/Articles/units`
Returns all enabled EAN → UnitId mappings (used internally for scanner lookup).

#### `GET /api/Articles/by-ean/{ean}`
Returns a single `DtoArticleUnit` matching the given EAN (unit or box), or 404.

#### `GET /api/Articles/{unitId}`
Returns a single `DtoArticleUnit` by its `UnitId`, or 404.

---

### Dispatch Sheets (Beladelisten)

#### `GET /api/DispatchSheets`
Returns all dispatch sheets ordered by name.
```json
[{ "id": 1, "name": "Messe Frankfurt 2026" }]
```

#### `POST /api/DispatchSheets`
Create a new dispatch sheet. `id` must be `null`.
```json
{ "id": null, "name": "Messe München 2026" }
```
Response: `201 Created` with the created object and a `Location` header.

#### `GET /api/DispatchSheets/{id}`
Get a single dispatch sheet by ID, or 404.

#### `PUT /api/DispatchSheets/{id}`
Update name. Body same as POST. Returns `204 No Content`.

#### `DELETE /api/DispatchSheets/{id}`
Delete a dispatch sheet. All associated `DispatchSheetRequiredUnits` are cascade-deleted. Returns `204 No Content`.

#### `GET /api/DispatchSheets/{dispatchSheetId}/units`
Returns all enabled `ArticleUnit` records combined with the required counts configured for this dispatch sheet. Used in the Config screen to populate the Sollbestand table.
```json
[{ "id": 101, "unitId": 101, "articleNr": "12345", "articleDisplayName": "...", "unitWeight": 500, "ean": "...", "requiredCount": 3 }]
```
Articles with no required count have `"requiredCount": null`.

#### `POST /api/DispatchSheets/{dispatchSheetId}/required-units`
Set (insert or update) the required count for one article unit.
```json
{ "unitId": 101, "count": 5 }
```
Returns `204 No Content`. Returns 404 if the dispatch sheet does not exist. `count` must be > 0.

#### `GET /api/DispatchSheets/{dispatchSheetId}/required-units`
Returns a dictionary `{ "unitId": requiredCount, … }` for all required units.

#### `DELETE /api/DispatchSheets/{dispatchSheetId}/required-units/{unitId}`
Remove the required count for a unit. Idempotent — returns `204 No Content` even if no entry existed.

---

### Scan Sessions

#### `POST /api/ScanSessions?sessionType={type}&dispatchSheetId={id}`
Create a new scan session. Query parameters:

| Parameter | Type | Required |
|---|---|---|
| `sessionType` | `ProcessDispatchList` \| `Inventory` | yes |
| `dispatchSheetId` | int | required when `ProcessDispatchList`; must be absent for `Inventory` |

Response: `201 Created` with `DtoScanSession`.

```json
{ "id": 7, "startedAt": "2026-04-25T10:00:00", "sessionType": "ProcessDispatchList", "dispatchSheetId": 1, "updatedAt": "2026-04-25T10:00:00" }
```

#### `GET /api/ScanSessions/current`
Returns the most recently updated scan session, or `404` if none exists. Used on app startup to resume the previous session.

#### `GET /api/ScanSessions/{id}`
Get a scan session by ID, or 404.

#### `GET /api/ScanSessions/{id}/articles`
Returns all scanned articles for the session, combined with unscanned but required articles (for `ProcessDispatchList` sessions).

```json
[{
  "id": 12,
  "unitId": 101,
  "articleNr": "12345",
  "articleDisplayName": "Muster Artikel",
  "unitWeight": 500,
  "ean": "4000001234567",
  "count": 3,
  "requiredCount": 5,
  "updatedAt": "2026-04-25T11:23:45"
}]
```
`count = 0` and `updatedAt = null` for articles that are in the dispatch sheet but have not been scanned yet. `requiredCount = null` for `Inventory` sessions.

#### `GET /api/ScanSessions/{id}/articles/excel`
Download the scan result as an `.xlsx` file.

- Response: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Filename: `result.xlsx`
- Columns for `Inventory`: Art.Nr., Artikel, Gewicht, EAN, Bestand
- Columns for `ProcessDispatchList`: Art.Nr., Artikel, Gewicht, EAN, Bestand, **Soll**, **Fehlt**
- Rows sorted by `ArticleNr`, then `UnitWeight`

---

### Barcode Scanner

#### `GET /api/BarcodeScanner/status`
Returns the current connection state of the physical scanner.
```json
{ "isConnected": true, "message": "Scanner ist verbunden" }
```

---

### SignalR Hub

Hub URL: `/hubs/notification`

| Event (server → client) | Parameters | When fired |
|---|---|---|
| `BarcodeScanned` | `ean: string` | A barcode was successfully processed |
| `BarcodeError` | `ean: string, errorMessage: string` | Scan failed (unknown EAN, no active session, etc.) |
| `ScannerStatusChanged` | `isConnected: bool` | Scanner connects or disconnects |

---

## Frontend

### Routes

| Path | Component | Purpose |
|---|---|---|
| `/scan-session` | `ScanSession` | Main scan view, start sessions, live article list |
| `/config` | `RequiredStockSetup` | Manage dispatch sheets, set required counts, upload articles |

### State — `ScanSessionStore` (NgRx Signal Store, root-provided)

The single shared store initialises on app startup (`withHooks.onInit`) and:

1. Loads the current scan session (`GET /api/ScanSessions/current`)
2. Loads all dispatch sheets
3. Loads scanner status
4. Opens the SignalR connection and registers listeners

**Signals exposed:**

| Signal | Type | Description |
|---|---|---|
| `selectedScanSession` | `DtoScanSession \| null` | Active session |
| `scanSessionArticles` | `DtoScanSessionArticle[]` | Articles for current session |
| `dispatchSheets` | `DtoDispatchSheet[]` | All dispatch sheets |
| `dispatchSheetName` | `string \| null` | Name of the active session's dispatch sheet |
| `barcodeScannerStatus` | `BarcodeScannerStatus \| null` | Scanner connection state |
| `isLoading` | `boolean` | Any async operation in progress |
| `hasScanSession` | `boolean` (computed) | |
| `scanSessionId` | `number \| null` (computed) | |
| `isScannerConnected` | `boolean` (computed) | |

**Methods:**

| Method | Triggered by |
|---|---|
| `loadCurrentScanSession()` | App init, after new session created |
| `startNewScanSession({ sessionType, dispatchSheetId })` | User click |
| `reloadScanSessionArticles()` | `BarcodeScanned` event, component `OnInit` |
| `loadDispatchSheets()` | App init, after dispatch sheet created |
| `loadBarcodeScannerStatus()` | App init, `ScannerStatusChanged` event |
| `createDispatchSheet(name)` | Config screen |
| `createDispatchSheetAndStartScanSession(name)` | Convenience (not used in current UI) |

### API Service Layer

```
Component / Store
    └── Hand-written service (client/src/app/api/)
            └── Generated OpenAPI service (*OpenApi, *.openapi.service)  ← DO NOT EDIT
                    └── HTTP to ASP.NET Core
```

| Hand-written service | Wraps |
|---|---|
| `ScanSessionsService` | `ScanSessionsOpenApi` |
| `DispatchSheetsService` | `DispatchSheetsOpenApi` |
| `ArticlesService` | `ArticlesOpenApi` |
| `BarcodeScannerService` | `BarcodeScannerOpenApi` |
| `SignalrService` | `@microsoft/signalr` |

Regenerate OpenAPI services (server must be running):
```cmd
cd client/src/app/api/openapi
gen-backend.cmd
```

### Components

#### `ScanSession` (`/scan-session`)
- Reads from `ScanSessionStore` via `inject(ScanSessionStore)`.
- Displays article list sorted by `updatedAt` descending (newest scans at top).
- **Beladung dialog** — user selects a dispatch sheet, then `store.startNewScanSession({ sessionType: ProcessDispatchList, ... })` is called.
- **Bestandsaufnahme dialog** — no dispatch sheet, calls `store.startNewScanSession({ sessionType: Inventory, dispatchSheetId: null })`.
- **Excel export** — calls `ScanSessionsService.getScanSessionArticlesExcel()` and triggers a browser download using a temporary `<a>` element.

#### `RequiredStockSetup` (`/config`)
- Manages dispatch sheets and their required counts.
- Inline editing: click on a count value → number input appears → ✓ / ✗ buttons.
- Article list sorted: items with a required count first, then alphabetically.
- **Artikelliste upload** — opens a file picker (`.json` only), uploads via `ArticlesService.uploadArticles()`.
- Buttons "Konfiguration exportieren/importieren" exist in the template but are not yet implemented.

---

## WPF Desktop Integration

### Purpose
`server/messe-app/` is a Windows WPF application that packages `messe-server` as a self-contained desktop product. The user sees a full-screen WebView2 window showing the Angular SPA — there is no separate browser needed.

### Namespace
`Herrmann.MesseApp.Windows` — distinct from the API's `Herrmann.MesseApp.Server`.

### Start-up sequence

```
MainWindow_Loaded
  → EnsureCoreWebView2Async()
  → FindServerExecutable()          looks for {AppBase}/server/messe-server.exe
  → Process.Start(messe-server.exe) WorkingDirectory = server executable directory
  → WaitForServerAsync()            polls http://localhost:5227 every 500 ms, up to 30 s
  → WebView.Source = "http://localhost:5227"
  → StatusBar: "Server läuft" (green)
```

### Shutdown
`Window_Closing` calls `serverProcess.Kill(entireProcessTree: true)` — this also terminates any child processes spawned by the server.

### Expected deployment layout
```
messe-app.exe
server/
  messe-server.exe
  messeapp.db           ← SQLite database (created on first run)
  logs/                 ← Serilog log files
```

The `.csproj` has a `ProjectReference` to `messe-server`, so both projects build together from `server/server.sln`.

---

## Deployment

### Prerequisites

| Component | Requirement |
|---|---|
| .NET 9 SDK | server and WPF app |
| Node.js + npm | Angular client |
| Windows 10/11 | WPF app (WebView2) |
| WebView2 Runtime | usually pre-installed on Windows 10/11 |

### Development

```bash
# Terminal 1 — API
cd server/messe-server
dotnet run

# Terminal 2 — Angular
cd client
npm start
# Open http://localhost:4200
```

The Angular dev server proxies `/api/*` and `/hubs/*` to `http://localhost:5227` (configured in `client/proxy.conf.json`). CORS is not needed.

### Production build

```bash
# 1. Build Angular into server's wwwroot
cd client
npm run build
# Output: client/dist/ — copy contents to server/messe-server/wwwroot/

# 2. Publish server
cd server/messe-server
dotnet publish -c Release -o ./publish

# 3. Publish WPF host (Windows only)
cd server/messe-app
dotnet publish -c Release -o ./publish
```

The server serves the Angular SPA as static files (`UseDefaultFiles` + `UseStaticFiles`). In production there is no CORS issue because both app and API run on the same origin (port 5227).

The Angular `index.html` is always served with `Cache-Control: no-store` so the browser always fetches the latest version.

### First-run database initialisation

On startup, `Program.cs` calls `dbContext.Database.EnsureCreatedAsync()`. This creates `messeapp.db` in the server's working directory if it doesn't exist. No migrations are used; changing the schema requires deleting the database file and restarting.

### Initial article data

After first deployment, upload the article catalogue through the UI:

1. Open `/config`
2. Click **Artikelliste** → select the `.json` export file
3. The server deletes all existing `ArticleUnits` and re-imports from the uploaded file

A sample data file is included at `server/messe-server/testdata/articles-with-units-*.json`.

### Logging

Serilog writes to both console and rolling daily files under `logs/messe-server-{date}.txt` (7-day retention). Configure log levels in `appsettings.json` under the `Serilog` key.

### Port configuration

Change the port in `server/messe-server/Properties/launchSettings.json` and in `appsettings.json` (`"Urls": "http://*:5227"`).  
If using the WPF host, also update `ServerUrl` in `MainWindow.xaml.cs`.
