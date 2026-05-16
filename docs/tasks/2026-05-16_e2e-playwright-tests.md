# [TASK] E2E Tests with Playwright

> **Status:** Draft  
> **Date:** 2026-05-16  
> **Author:** alexpuh  
> **Jira link:** *(fill in after creation)*

---

## Summary

Add a Playwright end-to-end test suite that covers all three application screens (`/scan-session`, `/config`, `/combined-view`) against a real running backend. Barcode scan events are simulated via a new `POST /api/Debug/scan` endpoint that is only available in the `Development` environment.

---

## Background

The application currently has backend unit tests (`messe-server.Tests`) but no automated end-to-end tests. This means that UI workflows — session creation, article display, scan result updates, combined view — are only verified manually. Playwright E2E tests will catch regressions in the full request-response cycle (Angular → API → SQLite → SignalR → Angular Store → UI) before release.

---

## Affected components

| Layer | Component | Type of change |
|---|---|---|
| Server — new controller | `DebugController` (`POST /api/Debug/scan`) | New — Development only |
| Server — API | `messe-server` | New endpoint (no schema change) |
| New project | `e2e/` at repository root | New Playwright npm project |
| CI | `.github/workflows/main.yml` | New `e2e` job |
| WPF host | — | Not affected |
| Excel | — | Not affected |
| SignalR | — | No new events (Debug endpoint reuses existing `BarcodeScanned` via `SignalNotificationService`) |

---

## User stories

```
As a developer,
I want automated Playwright E2E tests for all three application screens,
so that UI regressions in the full request-response cycle are caught before release.
```

---

## Acceptance criteria

**AC-1: Test project setup**
```gherkin
Given  the repository is checked out and the server is running in Development mode
When   `npm run test` is run inside `e2e/`
Then   all Playwright tests are discovered and pass
```

**AC-2: Config screen — create Beladeliste (happy path)**
```gherkin
Given  the app is open at /config
When   the user enters a name for a new Beladeliste and confirms
Then   the new Beladeliste appears in the list
```

**AC-3: Config screen — set Sollbestand (happy path)**
```gherkin
Given  a Beladeliste exists and at least one article is loaded
When   the user clicks an article row, enters a Sollbestand count, and saves
Then   the article row shows the updated required count
```

**AC-4: Scan session — start Beladung (happy path)**
```gherkin
Given  a Beladeliste with at least one required article exists
When   the user clicks "Neue Beladung starten", selects the Beladeliste, and confirms
Then   a scan session is started and the article list shows articles with Soll column
```

**AC-5: Scan session — start Beladung without selecting Beladeliste (error)**
```gherkin
Given  the "Neue Beladung starten" dialog is open
When   the user tries to confirm without selecting a Beladeliste
Then   the start button is disabled or an error message is shown
```

**AC-6: Scan session — scan a barcode via Debug endpoint (happy path)**
```gherkin
Given  a Beladung session is active and an article with a known EAN exists
When   POST /api/Debug/scan?ean={known_ean} is called
Then   the article list updates via SignalR and the article count increments to 1
```

**AC-7: Scan session — Bestandsaufnahme Stand (happy path)**
```gherkin
Given  no preconditions (no Beladeliste required)
When   the user starts a Bestandsaufnahme with Ort = Stand
Then   the session is started and the article list shows only the "Ist" column (no Soll, no Fehlt)
```

**AC-8: Scan session — Bestandsaufnahme Lager (happy path)**
```gherkin
Given  a Beladeliste with at least one required article exists
When   the user starts a Bestandsaufnahme with Ort = Lager and selects the Beladeliste
Then   the session is started and the article list shows Ist, Soll, and Fehlt columns
```

**AC-9: Combined view — display Messeabschluss (happy path)**
```gherkin
Given  one completed Stand session and one completed Lager session exist
When   the user opens /combined-view, selects both sessions, and clicks "Anzeigen"
Then   the combined table is displayed with columns: Stand Ist, Lager Ist, Gesamt, Soll, Fehlt
```

---

## Implementation notes (preliminary)

### New server endpoint: `POST /api/Debug/scan`

A `DebugController` available **only in `Development` environment**. It accepts an EAN and reuses the existing scan processing pipeline:

```
POST /api/Debug/scan?ean={ean}
```

Internally it:
1. Calls `ScanSessionService.AddBarcodeAsync(currentSessionId, ean)` — the same method the physical scanner triggers
2. The service fires `SignalNotificationService.SendBarcodeScanned` / `SendBarcodeError` as usual
3. Returns the result (success: `200 OK`; unknown EAN: `400 Bad Request` with error message; no active session: `400 Bad Request`)

The controller must be registered conditionally — either:
- Only registered in `Program.cs` when `app.Environment.IsDevelopment()`, or
- Decorated with a custom `[Development]` filter that returns `404` in Production

> **Important:** This endpoint must never be reachable in Production. The recommended approach is to guard registration in `Program.cs` with `if (app.Environment.IsDevelopment())`.

The Debug endpoint uses the "current session" — the same one the physical scanner would target (most recently updated `ScanSession`). It does **not** accept a `sessionId` parameter, to stay consistent with how the real scanner picks the active session.

### E2E project structure

```
e2e/
  package.json            ← @playwright/test
  playwright.config.ts    ← baseURL: http://localhost:4200, browser: Chromium only
  tests/
    config.spec.ts        ← /config screen tests
    scan-session.spec.ts  ← /scan-session tests
    combined-view.spec.ts ← /combined-view tests
  fixtures/
    articles.json         ← minimal subset extracted from docs/tasks/articles.json (created during implementation)
  helpers/
    api.ts                ← typed helpers for seeding data via the API (create dispatch sheet, upload articles, etc.)
```

### Test environment setup

Tests require both the Angular dev server and the .NET backend to be running:

| Process | Command | Port |
|---|---|---|
| .NET backend | `dotnet run` with `ASPNETCORE_ENVIRONMENT=Development` and a temp DB | 5227 |
| Angular dev server | `npm start` in `client/` | 4200 |

Playwright's `webServer` configuration supports **multiple servers**. Both servers should be declared in `playwright.config.ts` so Playwright starts and waits for them automatically.

**Database isolation:** Pass `ConnectionStrings__DefaultConnection` as an environment variable to `dotnet run` to point to a temporary SQLite file (e.g., `/tmp/messeapp-e2e.db` in CI, or a `tmp/` subfolder locally). A fresh file is created automatically on each run because the server calls `EnsureCreatedAsync`.

### Test data seeding

Tests use `beforeAll` hooks to seed data via API calls:
1. `POST /api/Articles/upload-articles` — upload `fixtures/articles.json`
2. `POST /api/DispatchSheets` — create a test Beladeliste
3. `POST /api/DispatchSheets/{id}/required-units` — set required counts for a subset of articles

There is **no teardown** required per test run since each CI run uses a fresh temp database. For local development, the temp file can be deleted between runs, or tests can be written to be idempotent (use uniquely named dispatch sheets).

### `fixtures/articles.json` — real catalogue extract

**Source:** `docs/tasks/articles.json` in the repository (full production catalogue, placed by the task owner). This file is **temporary** — it will be deleted after implementation is complete.

**What the developer must do:**

1. Open `docs/tasks/articles.json` and pick a small number of articles (enough to cover the test scenarios).
2. Create `e2e/fixtures/articles.json` as a minimal subset — only the selected articles, in the same JSON format.
3. Declare the chosen EAN codes and article numbers as constants in `helpers/constants.ts` (e.g., `TEST_EAN_UNIT`, `TEST_EAN_BOX`, `TEST_ARTICLE_NR`).
4. After all E2E tests are implemented and passing, **delete `docs/tasks/articles.json`** from the repository.

**Selection criteria** — the subset must include at least:
- One article unit with both `EanUnit` and `EanBox` defined
- One article unit with only `EanUnit` (no box EAN)

**Guard:** If `e2e/fixtures/articles.json` does not exist when the test suite starts, `globalSetup` must throw:
```
ERROR: e2e/fixtures/articles.json not found.
Create it from docs/tasks/articles.json before running E2E tests.
```

### Playwright configuration highlights

```typescript
// playwright.config.ts (sketch)
export default defineConfig({
  testDir: './tests',
  use: {
    baseURL: 'http://localhost:4200',
    browserName: 'chromium',
    headless: true,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  webServer: [
    {
      command: 'dotnet run --project ../server/messe-server/messe-server.csproj',
      url: 'http://localhost:5227/api/BarcodeScanner/status',
      reuseExistingServer: !process.env.CI,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ConnectionStrings__DefaultConnection: 'DataSource=../tmp/messeapp-e2e.db',
      },
    },
    {
      command: 'npm start --prefix ../client',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env.CI,
    },
  ],
});
```

### Database schema changes
- [ ] Not required — the Debug endpoint is additive and does not change the schema.

### New SignalR events
- Not applicable — the Debug endpoint reuses existing `BarcodeScanned` / `BarcodeError` events.

### API backward compatibility
- The new `POST /api/Debug/scan` endpoint returns `404` in Production (not registered), so no regeneration of the OpenAPI client is needed for production use.
- If the OpenAPI spec is regenerated while the server is running in Development mode, the Debug endpoint will appear in the spec. This is acceptable — it is a test-only endpoint and will not affect the production client.

---

## Contradictions with existing documentation

| # | What the requirements say | How it works today | Resolution |
|---|---|---|---|
| — | No contradictions | The feature is purely additive — new test project and a dev-only controller | — |

---

## Open questions

> All questions resolved.

~~**Q1:** Should the `create-release` CI job in `main.yml` be blocked by the new `e2e` job?~~  
**Resolved:** Yes — add `e2e` to `needs: [build-client, build-app, test-server, e2e]` in `create-release`.

~~**Q2:** How should the "current session" be resolved in the Debug endpoint when no session exists?~~  
**Resolved:** Return `400 Bad Request` with message `"Keine aktive Session"`.

---

## Out of scope

- Testing Excel export download content (Playwright can intercept the download but verifying `.xlsx` content requires additional tooling — left for a future task).
- Testing real physical scanner behaviour (SerialPort is hardware-dependent — covered conceptually by unit tests in `messe-server.Tests`).
- Visual regression testing (screenshot comparison).
- Mobile viewport or cross-browser testing (Chromium only per decision).
- Testing the WPF desktop host (`messe-app`) — it is a Windows-only WebView2 shell.

---

## Technical risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `Debug` endpoint accidentally accessible in Production | Low | High | Guard registration with `if (app.Environment.IsDevelopment())` in `Program.cs`; add an integration test that verifies `404` when `ASPNETCORE_ENVIRONMENT=Production` |
| `webServer` startup race condition (Angular dev server slow to start) | Medium | Medium | Set `timeout` on the Angular `webServer` entry (e.g., 120 s) and use `reuseExistingServer: true` locally |
| SQLite temp file left behind after local runs | Low | Low | Document cleanup command (`rm tmp/messeapp-e2e.db`) in `e2e/README.md` |
| SignalR event arrives before Playwright assertion starts | Medium | Medium | Use `page.waitForResponse` or `expect(locator).toHaveText(...)` with built-in retry to handle async SignalR updates |
| CI runner has no Playwright browsers cached | Medium | Medium | Add `npx playwright install --with-deps chromium` step to the CI job |
