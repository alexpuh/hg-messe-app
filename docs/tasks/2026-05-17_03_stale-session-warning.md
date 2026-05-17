# [TASK] Stale Session Warning

> **Status:** Draft  
> **Date:** 2026-05-17  
> **Author:** requirements-analyst  
> **Jira link:** *(fill in after creation)*

---

## Summary

When the active scan session was started on a previous day (not today), display a visual warning indicator next to the "Gestartet: DD.MM.YYYY" label. The warning disappears automatically after the first successful scan is received in the current UI session.

---

## Background

An operator may accidentally continue scanning into yesterday's session — especially after the app auto-resumes the most recently updated session on startup (or after using the new session-selection feature from Task 2). A prominent visual warning near the session start date makes this situation immediately visible and reduces the risk of data being recorded in the wrong session.

This task is independent of Task 2. The primary scenario is the app being launched the day after the last scanning session: `GET /api/ScanSessions/current` auto-resumes yesterday's session, and the operator should immediately see that the active session is from a previous day. Task 2 (manual session selection) adds a second scenario where the same warning applies, but is not a prerequisite.

---

## Affected components

| Layer | Component | Type of change |
|---|---|---|
| Store | `ScanSessionStore` | new computed signal `showStaleSessionWarning`; clear it on `BarcodeScanned` event |
| UI | `ScanSession` component | show warning badge/icon next to "Gestartet:" label; bind to store signal |
| WPF host | — | not affected |
| API | — | not affected |
| Excel | — | not affected |

---

## User stories

```
As an operator,
I want to see a warning when the active session was started on a previous day,
so that I notice before scanning that I may be using an outdated session.
```

---

## Acceptance criteria

**AC-1:** Warning is shown for a session started before today
```gherkin
Given the active session's startedAt date is before today (calendar date, local time)
When the /scan-session page renders the session header
Then a warning indicator (icon + short text) is shown next to the "Gestartet:" date
And the warning text reads: "Sitzung wurde nicht heute gestartet"
```

**AC-2:** Warning is NOT shown for a session started today
```gherkin
Given the active session's startedAt date equals today (calendar date, local time)
When the /scan-session page renders the session header
Then no warning indicator is shown
```

**AC-3:** Warning disappears after the first successful scan
```gherkin
Given the stale-session warning is currently visible
When the physical scanner (or debug scan) successfully scans a barcode
  (i.e., a BarcodeScanned SignalR event is received)
Then the warning indicator is no longer shown
And it does not reappear for the remainder of this UI session
```

**AC-4:** Warning reappears on page reload if no scan has occurred yet today
```gherkin
Given the active session was started before today
And no successful scan has been received in the current UI session
When the operator reloads the page or navigates away and back to /scan-session
Then the warning is shown again
```

> Note: the "has scanned today" flag is in-memory (store state). A page reload resets it. This is intentional — the warning serves as a prompt, not a persistent block.

**AC-5:** Warning is not shown when there is no active session
```gherkin
Given no active session exists (selectedScanSession is null)
When the /scan-session page renders
Then no stale-session warning is shown
```

---

## Implementation notes (preliminary)

### Store changes (`ScanSessionStore`)

**New state:**
- `_hadSuccessfulScanThisSession: boolean` — private, initially `false`; set to `true` on the first `BarcodeScanned` event.

**New computed signal:**
```typescript
showStaleSessionWarning = computed(() => {
  if (!this.selectedScanSession()) return false;
  if (this._hadSuccessfulScanThisSession()) return false;
  const startedAt = new Date(this.selectedScanSession()!.startedAt);
  const today = new Date();
  return startedAt.toDateString() !== today.toDateString();
});
```

**Existing `BarcodeScanned` handler** — add `this._hadSuccessfulScanThisSession.set(true)` before or after `reloadScanSessionArticles()`.

**Reset on session change** — when `loadCurrentScanSession()` or `resumeScanSession()` (Task 2) loads a new session, reset `_hadSuccessfulScanThisSession` to `false`. This ensures the warning re-evaluates for the newly selected session.

### UI

In `ScanSession` component, next to the `"Gestartet: {{ store.selectedScanSession()?.startedAt | germanDate }}"` label:

```html
@if (store.showStaleSessionWarning()) {
  <span class="stale-session-warning">
    <i class="pi pi-exclamation-triangle"></i>
    Sitzung wurde nicht heute gestartet
  </span>
}
```

Use PrimeNG `pi-exclamation-triangle` icon. Style with a warning colour (amber/orange, WCAG AA contrast required). The indicator must be visually prominent but not disruptive (inline, no modal).

### Date comparison

Use local calendar date (not UTC) for the "is today" check, since the operator is always on the same machine as the server and the trade-show context is entirely local. Compare `toDateString()` (browser locale-independent) or compare year/month/day components explicitly.

### Database schema changes
- [ ] No schema change required.

### New SignalR events
Not applicable — reuses the existing `BarcodeScanned` event.

### API backward compatibility
No API changes.

---

## Contradictions with existing documentation

| # | What the requirements say | How it works today | Resolution |
|---|---|---|---|
| 1 | Show warning when session was not started today | No such indicator exists | Purely additive UI change — no conflict |

---

## Open questions

- [ ] **Q1:** Should the warning disappear immediately on scan, or only after the article list has reloaded (i.e., after `reloadScanSessionArticles()` completes)?  
  *Assumed: immediately on receipt of the `BarcodeScanned` event, before the article reload, for the best perceived responsiveness.*

- [ ] **Q2:** If the operator manually selects a session from the recent-sessions list (Task 2) that was started before today, should the warning appear immediately upon selection, or only after the page is next loaded?  
  *Assumed: immediately — `showStaleSessionWarning` is a computed signal that reacts to `selectedScanSession` changes in real time.*

---

## Out of scope

- Blocking scanning into a stale session (this is a warning, not a guard).
- Persisting the "had scan today" flag across page reloads.
- Changing the behaviour of the server or scanner pipeline.

---

## Technical risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Timezone edge case: server `startedAt` is UTC, browser comparison is local | Low | Low | `startedAt` is stored as `DateTime` (local server time) and displayed as-is; compare using local date on the client, consistent with how the date is already displayed |
