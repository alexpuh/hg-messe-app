# E2E Tests — Messe App

Playwright end-to-end test suite covering all three application screens.

## Prerequisites

- .NET 9 SDK
- Node.js 20+
- `e2e/fixtures/articles.json` must exist (see below)

## Setup

```bash
cd e2e
npm ci
npx playwright install --with-deps chromium
```

## Running tests

Tests require both the Angular dev server (`http://localhost:4200`) and the .NET backend (`http://localhost:5227`) to be running. Playwright starts both automatically:

```bash
cd e2e
npm test
```

On the first run Playwright starts `dotnet run` and `npm start` for you. Set `reuseExistingServer` to `true` (the default for non-CI runs) to reuse already-running servers.

### Headed mode (for debugging)

```bash
npm run test:headed
```

### Debug mode (step through)

```bash
npm run test:debug
```

## Test isolation

All tests run serially (`workers: 1`). Before each test run, the SQLite database
(`e2e/tmp/messeapp-e2e.db`) is deleted so every run starts from a clean state.

**`npm test` / `npm run test:headed` / `npm run test:debug`**: Database cleanup runs via the
`pretest` / `clean` npm scripts automatically.

**`run-e2e.ps1`**: The script additionally kills any stale `messe-server` process that may be
holding the DB file open, then delegates to `npm test` (or `npm run test:*`), which triggers the
same cleanup hooks. Both entry points end up using `npm` so cleanup behaviour is identical.

## Articles fixture

`e2e/fixtures/articles.json` contains a minimal subset of the production article catalogue required by the tests. It must exist before running tests.

The fixture includes:
- **Anis ganz 50g** (artNr 1100, unitId 1, EAN `4260011990035`) — primary scan target
- **Anis ganz 1000g** (artNr 1100, unitId 2, EAN `4260011990042`) — secondary unit
- **Anis gemahlen 50g** (artNr 1110, unitId 3, EAN `4260011990059`) — only EanUnit defined

## Screens covered

| Spec file | Screen | ACs covered |
|---|---|---|
| `tests/config.spec.ts` | `/config` | AC-2, AC-3 |
| `tests/scan-session.spec.ts` | `/scan-session` | AC-4, AC-5, AC-6, AC-7, AC-8 |
| `tests/combined-view.spec.ts` | `/combined-view` | AC-9 |

## Debug endpoint

AC-6 (barcode scan simulation) calls `POST /api/Debug/scan?ean={ean}`, which is only available when `ASPNETCORE_ENVIRONMENT=Development`. The `playwright.config.ts` sets this environment automatically for the `webServer` command.
