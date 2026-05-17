# [TASK] Artikelliste Empty Guard

> **Status:** Draft  
> **Date:** 2026-05-17  
> **Author:** requirements-analyst  
> **Jira link:** *(fill in after creation)*

---

## Summary

If no `ArticleUnit` records exist in the database, the user must be prevented from starting a new session or selecting an existing one. A clear inline message must direct the user to the Konfiguration screen to upload an Artikelliste first.

---

## Background

The scanner cannot function without an article catalogue: every barcode scan is resolved against `ArticleUnit.EanUnit` / `ArticleUnit.EanBox`. If the table is empty, all scans would fail with "EAN unknown". This guard makes the problem visible before the user attempts to scan, rather than after.

This is the foundational prerequisite for tasks 2 and 3 (session selection and stale-session warning), both of which involve resuming or interacting with existing sessions. Preventing session access when articles are absent ensures a consistent app state.

---

## Affected components

| Layer | Component | Type of change |
|---|---|---|
| API | `GET /api/Articles/count` | new endpoint |
| UI | `ScanSession` component | add empty-articles guard: disable start buttons, show inline warning |
| UI | `ScanSession` session-selection list (Task 2) | disable session entries when articles absent |
| Store | `ScanSessionStore` | new signal `hasArticles`, new method `loadArticlesStatus()`, called on `onInit` |
| WPF host | — | not affected |
| Excel | — | not affected |

---

## User stories

```
As an operator,
I want to see a clear message when no article catalogue is loaded,
so that I know I must upload one before I can start scanning.
```

---

## Acceptance criteria

**AC-1:** Empty guard is shown when no articles exist
```gherkin
Given no ArticleUnit records exist in the database
When the operator opens the /scan-session page
Then the "Neue Beladung starten" button is disabled
And the "Bestandsaufnahme starten" button is disabled
And an inline warning is shown:
  "Keine Artikelliste vorhanden. Bitte zuerst in der Konfiguration eine Artikelliste hochladen."
And the warning contains a clickable link that navigates to /config
```

**AC-2:** Guard is NOT shown when articles exist
```gherkin
Given at least one enabled ArticleUnit record exists
When the operator opens /scan-session
Then the start buttons are enabled
And no empty-articles warning is displayed
```

**AC-3:** Guard reacts dynamically after upload
```gherkin
Given the operator is on /scan-session and the empty-articles warning is shown
When the operator navigates to /config, uploads a valid Artikelliste, and returns to /scan-session
Then the warning is no longer shown
And the start buttons are enabled
```

> Note for AC-3: the store reloads `articlesStatus` on every navigation to /scan-session (component `OnInit` triggers `loadArticlesStatus()`), so no SignalR event is needed.

---

## Implementation notes (preliminary)

### New API endpoint

`GET /api/Articles/count`  
Returns `{ "count": N }` — the number of enabled `ArticleUnit` records.  
Controller: `ArticlesController`. No parameters. Response DTO: `DtoArticlesCount { int Count }`.

> Alternative considered: reuse `GET /api/Articles/units` (returns EAN map) and check `length === 0` client-side. Rejected because that endpoint is intended as an internal scanner-lookup endpoint and may grow in payload size.

### Store changes (`ScanSessionStore`)

- New state: `articlesCount: number | null` (null = not yet loaded)
- New computed: `hasArticles = computed(() => (articlesCount() ?? 0) > 0)`
- New method: `loadArticlesStatus()` — calls `ArticlesService.getArticlesCount()`, writes result to `articlesCount`
- Call `loadArticlesStatus()` in `onInit` (alongside existing init calls)
- Call `loadArticlesStatus()` in `ScanSession` component's `OnInit` to refresh on navigation

### Service layer

Add `getArticlesCount(): Observable<DtoArticlesCount>` to `ArticlesService` (hand-written wrapper).  
Regenerate OpenAPI client after adding the endpoint.

### UI

In `ScanSession` component, bind button `[disabled]` to `!store.hasArticles()`.  
Show a `<p-message severity="warn">` (PrimeNG) with the German text and a `routerLink="/config"` anchor.  
The message is shown with `@if (!store.hasArticles() && !store.isLoading())`.

### Database schema changes
- [ ] No schema change required.

### New SignalR events
Not applicable.

### API backward compatibility
The new endpoint is additive. The OpenAPI client must be regenerated after the endpoint is added.

---

## Contradictions with existing documentation

| # | What the requirements say | How it works today | Resolution |
|---|---|---|---|
| 1 | Start buttons disabled when articles absent | No such guard exists | Add new guard — does not conflict |

---

## Open questions

- [ ] **Q1:** Should the guard also block navigation to `/config` if articles are empty, or is `/config` always accessible (to allow the upload)? — *Assumed: `/config` is always accessible; the guard only blocks session-related actions.*
- [ ] **Q2:** Should `IsArticleDisabled = true` articles count toward the "has articles" check? — *Assumed: only enabled articles count (same filter the scanner uses: `IsArticleDisabled = false AND IsUnitDisabled = false`).*

---

## Out of scope

- Validating the content of the Artikelliste (article count thresholds, required fields) — only presence/absence is checked.
- Blocking access to existing session data (article list display) — the guard only blocks starting/resuming sessions.

---

## Technical risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| OpenAPI client regeneration required after new endpoint | Certain | Low | Regenerate with `gen-backend.cmd` before implementing the store/service changes |
