# [TASK] Bestandsaufnahme mit Vergleich + Kombinierte Übersicht

> **Status:** Implemented  
> **Date:** 2026-05-15  
> **Author:** —  
> **Jira link:** *(fill in after creation)*

---

## Summary

Two related features are needed to support the real-world trade-show logistics workflow:

1. **Bestandsaufnahme mit Vergleich** — an inventory scan session that is optionally linked to a Beladeliste, showing Soll/Fehlt columns (like a Beladung but without the obligation to scan to target).
2. **Kombinierte Übersicht** — a view that merges two finished scan sessions (Stand + Lager) into a single table with separate columns per location, for export and on-screen review when the truck continues to the next exhibition.

---

## Background

A truck and trailer travel together to a trade show.

- The **truck** carries a pre-packed set of goods that are fully unloaded at the exhibition stand. This scan has no predefined list — it is a free-form inventory.
- The **trailer** acts as a mobile warehouse. It is loaded before departure according to a Beladeliste. During the exhibition, missing items are taken from the trailer.
- Workers occasionally perform a **Bestandsaufnahme des Lagers** to see the current stock vs. the original Beladeliste. The difference can be reordered from the warehouse.
- If the truck goes to **another exhibition** after this one (rather than returning home), all goods at the Stand *and* in the trailer must be scanned and combined into a single overview table.

---

## Affected components

| Layer | Component | Type of change |
|---|---|---|
| Data | `ScanSession.Ort` | New field — enum `Stand \| Lager`, selected when starting a session (**schema change**) |
| Data | `ScanSession.DispatchSheetId` | Allow nullable FK for `Inventory` sessions (no schema change — field already exists) |
| API | `POST /api/ScanSessions` | Add required `ort` parameter (`Stand \| Lager`); allow `dispatchSheetId` for `Inventory` session type |
| API | `GET /api/ScanSessions` | New endpoint — list all sessions (required for session picker) |
| API | `GET /api/ScanSessions/{id}/articles` | Return Soll/Fehlt when `Inventory` session has `DispatchSheetId` set |
| API | `GET /api/ScanSessions/{id}/articles/excel` | Include Soll/Fehlt columns when `Inventory` + `DispatchSheetId` |
| API | `GET /api/ScanSessions/combined` | New endpoint — merge two sessions into one table |
| API | `GET /api/ScanSessions/combined/excel` | New endpoint — combined Excel export |
| UI | Bestandsaufnahme dialog | Add optional Beladeliste selector |
| UI | ScanSession screen | Show Soll/Fehlt columns when Inventory session has a linked Beladeliste |
| UI | New `CombinedView` component (`/combined-view`) | new route + component |
| Excel | Combined export template | New format with Stand / Lager / Gesamt columns |
| WPF host | — | Not affected |

---

## User stories

```
As a warehouse worker at the exhibition,
I want to scan the trailer inventory and see what is missing compared to the original Beladeliste,
so that I can identify and reorder missing items.
```

```
As a logistics coordinator,
I want to combine the Stand scan and the trailer scan into one overview table,
so that I can plan the goods for the next exhibition when the truck does not return home.
```

---

## Acceptance criteria

**AC-1 — Bestandsaufnahme Lager: Beladeliste required**
```gherkin
Given  the user opens the "Bestandsaufnahme starten" dialog
And    selects Ort = "Lager"
When   the user selects a Beladeliste and clicks "Starten"
Then   the server creates an Inventory session with Ort = Lager and the selected DispatchSheetId
And    the article list shows Soll and Fehlt columns (same logic as ProcessDispatchList)
And    articles that are in the Beladeliste but not yet scanned appear with count = 0
```

**AC-2 — Bestandsaufnahme Lager: Beladeliste is mandatory**
```gherkin
Given  the user opens the "Bestandsaufnahme starten" dialog
And    selects Ort = "Lager"
When   no Beladeliste is selected
Then   the "Starten" button is disabled
And    the server rejects POST /api/ScanSessions with 400 if DispatchSheetId is absent and Ort = Lager
```

**AC-3 — Bestandsaufnahme Stand: Beladeliste forbidden**
```gherkin
Given  the user opens the "Bestandsaufnahme starten" dialog
And    selects Ort = "Stand"
Then   the Beladeliste selector is not shown
And    the server rejects POST /api/ScanSessions with 400 if DispatchSheetId is provided and Ort = Stand
```

**AC-4 — Bestandsaufnahme Stand: article list without Soll/Fehlt**
```gherkin
Given  an Inventory session with Ort = Stand (DispatchSheetId = null)
When   the article list is displayed
Then   only the Ist column is shown (no Soll, no Fehlt)
```

**AC-5 — Bestandsaufnahme Lager: Excel with comparison**
```gherkin
Given  an Inventory session with Ort = Lager and a linked DispatchSheetId
When   the user clicks "Excel exportieren"
Then   the downloaded .xlsx includes columns: Art.Nr., Artikel, Gewicht, EAN, Bestand, Soll, Fehlt
And    Fehlt is only populated when Bestand < Soll
```

**AC-6 — Session list API**
```gherkin
Given  multiple scan sessions exist in the database
When   GET /api/ScanSessions is called
Then   the response returns all sessions ordered by UpdatedAt descending
And    each session includes: id, sessionType, ort, dispatchSheetId, startedAt, updatedAt
```

**AC-7 — Combined view: session picker**
```gherkin
Given  at least one Stand session and one Lager session exist
When   the user opens the "Kombinierte Übersicht" section
Then   the UI shows two session dropdowns: "Stand" and "Lager"
And    the most recent Stand session is pre-selected in the Stand dropdown
And    the most recent Lager session is pre-selected in the Lager dropdown
And    each dropdown only shows sessions of the corresponding Ort type
```

**AC-8 — Combined view: merged table**
```gherkin
Given  a Stand session and an Lager session are selected in the combined view
When   the user clicks "Anzeigen"
Then   the table shows one row per unique article (union of both sessions)
And    columns are: Art.Nr., Artikel, Gewicht, EAN, Stand (Ist), Lager (Ist), Gesamt, Soll, Fehlt
And    Soll comes from the Lager session's linked Beladeliste
And    Fehlt = Soll − Gesamt (only shown when Gesamt < Soll, otherwise blank)
And    articles present in only one session show 0 in the other session's Ist column
And    Gesamt = Stand (Ist) + Lager (Ist)
```

**AC-9 — Combined view: Excel export**
```gherkin
Given  a Stand session and an Lager session are combined in the combined view
When   the user clicks "Excel exportieren"
Then   the downloaded .xlsx matches the on-screen table structure
And    columns are: Art.Nr., Artikel, Gewicht, EAN, Stand (Ist), Lager (Ist), Gesamt, Soll, Fehlt
And    the Stand column header includes the session timestamp (e.g. "Stand — 15.05.2026 10:32")
And    the Lager column header includes the session timestamp (e.g. "Lager — 15.05.2026 11:45")
```

---

## Implementation notes (preliminary)

### Feature 1: Bestandsaufnahme mit Vergleich

#### Database schema changes
- **Schema change required:** add `Ort` enum field (`Stand | Lager`) to `ScanSession`. Requires database deletion and recreation (`EnsureCreated`, no EF Core migrations).
- `ScanSession.DispatchSheetId` — already a nullable FK, no structural change needed; only API validation changes.

#### Server — `ScanSessionService.GetScanSessionArticlesAsync`
Currently merges "all scanned articles + unscanned required units" only when `SessionType == ProcessDispatchList`. Extend to also apply when `Ort == Lager` (which guarantees `DispatchSheetId != null`).

#### Server — `ScanSessionExcelExportService`
The `showExpectation` flag is currently `sessionType == ProcessDispatchList`. Change to:
```csharp
bool showExpectation = session.SessionType == SessionType.ProcessDispatchList
                    || session.Ort == Ort.Lager;
```

#### Angular UI — session creation dialogs (both Beladung and Bestandsaufnahme)
- Add a required `<p-select>` for `Ort` (`Stand | Lager`) to both session-start dialogs.
- Pass `ort` to `store.startNewScanSession(...)`.

#### Angular UI — Bestandsaufnahme dialog
- When `Ort = Lager`: show a **required** `<p-select>` for Beladeliste; "Starten" is disabled until one is selected.
- When `Ort = Stand`: Beladeliste selector is **not rendered**; `dispatchSheetId` is always `null`.
- Pass `ort` and `dispatchSheetId` to `store.startNewScanSession({ sessionType: Inventory, ort, dispatchSheetId })`.
- The dispatch sheet list is already available in `ScanSessionStore.dispatchSheets`.

#### Server — API validation rules
| `Ort` | `SessionType` | `DispatchSheetId` | Result |
|---|---|---|---|
| `Stand` | `Inventory` | absent/null | ✅ allowed |
| `Stand` | `Inventory` | provided | ❌ 400 Bad Request |
| `Lager` | `Inventory` | provided | ✅ allowed |
| `Lager` | `Inventory` | absent/null | ❌ 400 Bad Request |
| `Lager` | `ProcessDispatchList` | provided | ✅ allowed (unchanged) |
| `Stand` | `ProcessDispatchList` | provided | ❌ 400 Bad Request (loading list at Stand makes no sense) |

#### Angular UI — ScanSession article list
- Show Soll/Fehlt columns when `selectedScanSession.ort === 'Lager'` (covers both `ProcessDispatchList` and `Inventory` with Beladeliste).

---

### Feature 2: Kombinierte Übersicht

#### New API endpoint — list sessions
```
GET /api/ScanSessions
→ DtoScanSession[]  ordered by UpdatedAt descending
```

#### New API endpoint — combined articles
```
GET /api/ScanSessions/combined?sessionAId={id}&sessionBId={id}
→ DtoCombinedArticle[]
```

`DtoCombinedArticle`:
```json
{
  "unitId": 101,
  "articleNr": "12345",
  "articleDisplayName": "Muster Artikel",
  "unitWeight": 500,
  "ean": "4000001234567",
  "countStand": 3,
  "countAnhaenger": 1,
  "total": 4,
  "requiredCount": 6,
  "fehlt": 2
}
```
`fehlt` = `requiredCount − total` when `total < requiredCount`, otherwise `null`. `requiredCount` comes from the Lager session's linked Beladeliste.

#### New API endpoint — combined Excel
```
GET /api/ScanSessions/combined/excel?sessionAId={id}&sessionBId={id}
→ application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
```

Excel columns: `Art.Nr.` | `Artikel` | `Gewicht` | `EAN` | `Stand (Ist) — {timestamp}` | `Lager (Ist) — {timestamp}` | `Gesamt` | `Soll` | `Fehlt`

Session timestamp format: `dd.MM.yyyy HH:mm` (e.g. `Stand — 15.05.2026 10:32`)

#### Angular UI — Kombinierte Übersicht (`/combined-view`)
- New route `/combined-view` with a new component.
- Two `<p-select>` dropdowns populated from `GET /api/ScanSessions`, filtered by `Ort`: one for `Stand` sessions, one for `Lager` sessions.
- "Anzeigen" button triggers the combined view.
- Table with columns: Art.Nr., Artikel, Gewicht, EAN, Stand (Ist), Lager (Ist), Gesamt, Soll, Fehlt.
- "Excel exportieren" button downloads the combined Excel.
- New hand-written service method in `ScanSessionsService` for the two new endpoints.
- OpenAPI client must be regenerated after adding the new endpoints.

### New SignalR events
None required for these features.

### API backward compatibility
- `POST /api/ScanSessions` — validation change (relaxing a constraint) is backward compatible.
- `GET /api/ScanSessions/{id}/articles` — response shape unchanged for existing session types without linked dispatch sheet.
- `GET /api/ScanSessions` — new endpoint, no breaking change.
- `GET /api/ScanSessions/combined` and `.../combined/excel` — new endpoints, no breaking change.
- OpenAPI client regeneration required after implementing the new endpoints.

---

## Contradictions with existing documentation

| # | What the requirements say | How it works today | Resolution |
|---|---|---|---|
| 1 | Bestandsaufnahme should optionally show Soll/Fehlt when linked to a Beladeliste | `Inventory` sessions always have `DispatchSheetId = null`; Soll/Fehlt columns are only shown for `ProcessDispatchList` | Allow `DispatchSheetId` for `Inventory`; derive `showExpectation` from presence of `DispatchSheetId`, not just session type |
| 2 | The UI needs a list of all past sessions for the session picker | Only `GET /api/ScanSessions/current` exists; no list endpoint | Add `GET /api/ScanSessions` |
| 3 | Sessions must be distinguishable as Stand or Lager in the combined view | `ScanSession` has no location field | Add `Ort` enum (`Stand \| Lager`) to `ScanSession` — selected at session start; **requires DB recreation** |

---

## Open questions

> All open questions resolved — see answers below.

- [x] **Q1:** Separate route `/combined-view`. *(→ new navigation entry, new Angular route)*
- [x] **Q2:** Combined view shows **Soll** (from Lager Beladeliste) and **Fehlt** (Soll − Gesamt) in addition to Stand | Lager | Gesamt. *(→ affects AC-8, AC-9, API DTO, Excel template)*
- [x] **Q3:** Always exactly one Stand session + one Lager session — structure is fixed.
- [x] **Q4:** No deletion — session history is always kept.

---

## Out of scope

- Concept of "closing" or "finalising" a session — sessions remain open indefinitely; the combined view is always a snapshot at query time.
- Real-time updates of the combined view while scanning is in progress.
- Multi-user concurrency — the application runs on a single computer with a single operator.
- Naming/labelling of sessions beyond Ort + timestamp.

---

## Technical risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `ScanSession.Ort` schema change requires DB recreation | Certain | Medium | Delete `messeapp.db` and restart server before first use |
| No DB schema change needed for `DispatchSheetId` — but existing `Inventory` sessions have it `null`; new behaviour only affects new sessions | Low | Low | No migration; existing sessions unaffected |
| Combined view endpoint could be slow if sessions have many articles | Low | Medium | Merge is in-memory after two indexed DB queries; acceptable for exhibition scale |
| OpenAPI client must be regenerated after adding new endpoints | Certain | Low | Run `gen-backend.cmd` after server-side implementation |
