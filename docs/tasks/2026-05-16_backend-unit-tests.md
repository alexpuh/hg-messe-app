# [TASK] Backend Unit Tests

> **Status:** Draft  
> **Date:** 2026-05-16  
> **Author:** alexpuh  
> **Jira link:** *(fill in after creation)*

---

## Summary

Add a dedicated xUnit test project `messe-server.Tests` to the solution that covers the business-logic services of `messe-server`. The goal is to verify correctness of the core workflows (scanning, session management, dispatch sheet operations, article lookup, Excel export) without touching hardware or controllers.

---

## Background

The backend currently has no automated tests. All service logic (barcode scan processing, session/article aggregation, dispatch sheet CRUD) is exercised only manually. Adding unit tests will catch regressions early and serve as living documentation of the expected behaviour.

---

## Affected components

| Layer | Component | Type of change |
|---|---|---|
| Server — new project | `messe-server.Tests` (xUnit, NSubstitute, EF Core SQLite in-memory) | New test project |
| Server — services | `ScanSessionService`, `ArticlesService`, `DispatchSheetService`, `ScanSessionExcelExportService` | Covered by tests (no code changes expected) |
| WPF host | — | Not affected |
| Client | — | Not affected |
| SignalR / API | — | Not affected |

---

## User stories

```
As a developer,
I want automated unit tests for the backend services,
so that regressions are caught immediately during development without manual testing.
```

---

## Scope

### In scope — services to test

| Service | Key methods to cover |
|---|---|
| `ScanSessionService` | `CreateScanSessionAsync`, `AddBarcodeAsync`, `GetScanSessionArticlesAsync`, `GetCombinedArticlesAsync` |
| `DispatchSheetService` | `AddAsync`, `UpdateAsync`, `DeleteAsync`, `SetRequiredUnitsAsync`, `DeleteRequiredUnitAsync`, `GetDispatchSheetArticleUnits` |
| `ArticlesService` | `TryFindEan`, `TryGetArticleUnit`, `ImportFromJsonFileAsync`, `GetAllEanUnits` |
| `ScanSessionExcelExportService` | `Generate`, `GenerateCombined` |

### Out of scope

- `BarcodeScannerService` / `BarcodeScannerBackgroundService` — depend on `SerialPort` (sealed class, real hardware). Skipped in this iteration; see Technical Risks for future refactoring note.
- `SignalNotificationService` — thin wrapper over `IHubContext`; no business logic.
- Controllers — HTTP-level testing is out of scope; service logic is already covered.

---

## Acceptance criteria

**AC-1: Test project setup**
```gherkin
Given  the solution is opened
When   `dotnet test` is run on the solution
Then   the test project `messe-server.Tests` is discovered and all tests pass
```

**AC-2: ScanSessionService — CreateScanSessionAsync invariants**
```gherkin
Given  sessionType = ProcessDispatchList and ort = Stand
When   CreateScanSessionAsync is called
Then   ArgumentException is thrown

Given  sessionType = ProcessDispatchList and dispatchSheetId = null
When   CreateScanSessionAsync is called
Then   ArgumentException is thrown

Given  sessionType = Inventory and ort = Lager and dispatchSheetId = null
When   CreateScanSessionAsync is called
Then   ArgumentException is thrown

Given  sessionType = Inventory and ort = Stand and dispatchSheetId is provided
When   CreateScanSessionAsync is called
Then   ArgumentException is thrown

Given  valid inputs (e.g. ProcessDispatchList + Lager + dispatchSheetId)
When   CreateScanSessionAsync is called
Then   a new ScanSession is persisted and the returned ID is > 0
```

**AC-3: ScanSessionService — AddBarcodeAsync**
```gherkin
Given  a valid sessionId and a known EAN
When   AddBarcodeAsync is called
Then   returns (true, "") and a ScannedArticle with QuantityUnits = 1 exists in the DB

Given  the same EAN is scanned again in the same session
When   AddBarcodeAsync is called a second time
Then   QuantityUnits is incremented to 2 (no duplicate ScannedArticle created)

Given  an unknown EAN
When   AddBarcodeAsync is called
Then   returns (false, "Artikel mit EAN ... nicht gefunden")

Given  a non-existent sessionId
When   AddBarcodeAsync is called
Then   returns (false, "Session ... nicht gefunden")
```

**AC-4: ScanSessionService — GetScanSessionArticlesAsync**
```gherkin
Given  a session with scanned articles and a linked DispatchSheet with required units
When   GetScanSessionArticlesAsync is called
Then   the result includes both scanned articles (with Count > 0) and unscanned required articles (with Count = 0 and RequiredCount set)

Given  a session with no scanned articles and no DispatchSheet
When   GetScanSessionArticlesAsync is called
Then   the result is an empty array

Given  a non-existent sessionId
When   GetScanSessionArticlesAsync is called
Then   returns null
```

**AC-5: ScanSessionService — GetCombinedArticlesAsync**
```gherkin
Given  a valid Stand session ID and a valid Lager session ID
When   GetCombinedArticlesAsync is called
Then   articles from both sessions are merged; Total = CountStand + CountAnhaenger

Given  a standSessionId that refers to a Lager session (wrong Ort)
When   GetCombinedArticlesAsync is called
Then   returns null

Given  a lagerSessionId that does not exist
When   GetCombinedArticlesAsync is called
Then   returns null
```

**AC-6: DispatchSheetService — CRUD**
```gherkin
Given  a new DispatchSheet name
When   AddAsync is called
Then   the sheet is persisted and returned with a valid Id

Given  an existing DispatchSheet
When   UpdateAsync is called with a new name
Then   returns true and the name is updated in the DB

Given  a non-existent ID
When   UpdateAsync or DeleteAsync is called
Then   returns false

Given  an existing DispatchSheet
When   DeleteAsync is called
Then   returns true and the record is removed
```

**AC-7: DispatchSheetService — SetRequiredUnitsAsync**
```gherkin
Given  a valid dispatchSheetId, unitId, and count > 0
When   SetRequiredUnitsAsync is called
Then   returns true and the RequiredCount is stored

Given  the same unitId is set again with a different count
When   SetRequiredUnitsAsync is called
Then   the existing record is updated (no duplicate created)

Given  count <= 0
When   SetRequiredUnitsAsync is called
Then   ArgumentOutOfRangeException is thrown

Given  a non-existent dispatchSheetId
When   SetRequiredUnitsAsync is called
Then   returns false
```

**AC-8: ArticlesService — EAN lookup**
```gherkin
Given  an ArticleUnit with a known EanUnit
When   TryFindEan is called with that EAN
Then   returns true and the matching DtoArticleUnit

Given  an ArticleUnit with a known EanBox
When   TryFindEan is called with the EanBox value
Then   returns true and the matching DtoArticleUnit

Given  an unknown EAN
When   TryFindEan is called
Then   returns false and articleUnit is null
```

**AC-9: ArticlesService — ImportFromJsonFileAsync**
```gherkin
Given  a valid JSON file with article data
When   ImportFromJsonFileAsync is called
Then   all ArticleUnits are persisted and the count returned matches

Given  a non-existent file path
When   ImportFromJsonFileAsync is called
Then   FileNotFoundException is thrown

Given  an empty JSON array
When   ImportFromJsonFileAsync is called
Then   returns 0 and the existing ArticleUnits are deleted
```

**AC-10: ScanSessionExcelExportService — Generate**
```gherkin
Given  a list of DtoScanSessionArticle items and showExpectation = true
When   Generate is called
Then   the output stream contains a valid .xlsx file with columns: Art.Nr., Artikel, Gewicht, EAN, Bestand, Soll, Fehlt

Given  showExpectation = false
When   Generate is called
Then   the output stream contains a valid .xlsx file without Soll and Fehlt columns (5 columns only)
```

**AC-11: ScanSessionExcelExportService — GenerateCombined**
```gherkin
Given  valid Stand and Lager sessions and a list of DtoCombinedArticle
When   GenerateCombined is called
Then   the output stream contains a valid .xlsx with worksheet name "Messeabschluss" and 9 columns
```

---

## Implementation notes (preliminary)

### Test project setup

- Create `server/messe-server.Tests/messe-server.Tests.csproj`
- Add to `server/server.sln`
- NuGet packages:
  - `xunit`
  - `xunit.runner.visualstudio`
  - `Microsoft.NET.Test.Sdk`
  - `NSubstitute`
  - `Microsoft.EntityFrameworkCore.Sqlite` (already in main project — reuse for in-memory SQLite)
  - `ClosedXML` (for Excel output assertions)
- Project reference: `<ProjectReference Include="..\messe-server\messe-server.csproj" />`

### DB helper pattern

Each test (or test class) should create a fresh `MesseAppDbContext` backed by an SQLite `:memory:` connection. Recommended pattern:

```csharp
private static MesseAppDbContext CreateDbContext()
{
    var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    var options = new DbContextOptionsBuilder<MesseAppDbContext>()
        .UseSqlite(connection)
        .Options;
    var ctx = new MesseAppDbContext(options);
    ctx.Database.EnsureCreated();
    return ctx;
}
```

> Important: keep the `SqliteConnection` alive for the lifetime of the test — closing it drops the in-memory database. Either hold a field reference or use a shared fixture.

### ArticlesService.TryFindEan / TryGetArticleUnit

These methods are **synchronous** and call `dbContext.ArticleUnits.FirstOrDefault(...)` directly — no `async`. Tests are straightforward: seed data, call method, assert result.

### ScanSessionExcelExportService

No DB dependency. Inject `NSubstitute.For<ILogger<ScanSessionExcelExportService>>()`. Call `Generate` / `GenerateCombined` with a `MemoryStream`. Open the result with `new XLWorkbook(stream)` and assert cell values.

### Naming convention

Test classes: `<ServiceName>Tests` (e.g. `ScanSessionServiceTests`).  
Test methods: `<Method>_<Scenario>_<ExpectedOutcome>` (e.g. `AddBarcodeAsync_UnknownEan_ReturnsFalse`).

### Database schema changes
- [ ] Not required — tests are additive.

### New SignalR events
- Not applicable.

### API backward compatibility
- Not applicable.

---

## Contradictions with existing documentation

| # | What the requirements say | How it works today | Resolution |
|---|---|---|---|
| — | No contradictions | Tests reflect current documented behaviour | — |

---

## Open questions

> All questions resolved.

~~**Q1:** Should `dotnet test` be added to a CI pipeline (GitHub Actions), or is this a local-dev-only concern for now?~~  
**Resolved:** Yes — add to GitHub Actions CI.

### CI integration notes

The existing workflow (`.github/workflows/main.yml`) has a `build-app` job on `windows-latest`.  
The recommended approach is to add a **separate `test-server` job** on `ubuntu-latest` (no WPF dependency, lighter runner):

```yaml
test-server:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.0.x
    - name: Run server unit tests
      run: dotnet test server/messe-server.Tests/messe-server.Tests.csproj --configuration Release --logger "trx;LogFileName=test-results.trx"
    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: test-results-${{ github.run_number }}
        path: "**/*.trx"
```

The `create-release` job should add `test-server` to its `needs` array so that a release is blocked by failing tests.

---

## Out of scope

- `BarcodeScannerService` / `BarcodeScannerBackgroundService` — `SerialPort` is a `sealed` class and cannot be mocked directly. Testability requires extracting an `ISerialPortWrapper` abstraction and refactoring the service — left for a dedicated future task.
- `SignalNotificationService` — no business logic, thin hub wrapper.
- Controllers (HTTP layer) — not in scope for this iteration.
- Frontend (Angular) — not affected.

---

## Technical risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| SQLite in-memory connection is closed prematurely (dropped DB) | Medium | High | Hold `SqliteConnection` in a field or `IDisposable` fixture; call `connection.Open()` before `EnsureCreated()` |
| `ArticlesService` methods are synchronous — EF Core change tracker may behave unexpectedly without `AsNoTracking` | Low | Low | Tests are read-only for lookup methods; write operations in other methods reset DB per test |
| ClosedXML in test output assertions may be brittle if formatting changes | Low | Low | Assert on content (cell values) only, not on style |
| `BarcodeScannerService` has unverified logic (retry, ACK/BEL) | Medium | Medium | Document as out of scope; create a follow-up task for `ISerialPortWrapper` refactoring |
