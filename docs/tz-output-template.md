# [TASK] <Short title>

> **Status:** Draft  
> **Date:** YYYY-MM-DD  
> **Author:** <name>  
> **Jira link:** *(fill in after creation)*

---

## Summary

*1–3 sentences: what needs to be done and why.*

---

## Background

*Where the task came from, what problem it solves, who requested it / who is the user.*

---

## Affected components

| Layer | Component | Type of change |
|---|---|---|
| Data | `ArticleUnit` / `DispatchSheet` / ... | add field / new entity / ... |
| API | `GET /api/...` | new endpoint / contract change |
| SignalR | event `XxxChanged` | new / parameter change |
| UI | `ScanSession` / `RequiredStockSetup` | new / update |
| WPF host | — | not affected |
| Excel | export template | — |

---

## User stories

```
As a <role>,
I want to <action>,
so that <goal>.
```

---

## Acceptance criteria

> Each criterion must be a verifiable statement.

**AC-1:**
```gherkin
Given  <precondition>
When   <action>
Then   <expected result>
```

**AC-2:**
...

---

## Implementation notes (preliminary)

*Technical notes for the developer: which services, which methods, important constraints.*

### Database schema changes
- [ ] Requires database deletion and recreation (`EnsureCreated`, no EF Core migrations)

### New SignalR events
*(If applicable — all three locations):*
- `SignalNotificationService.Send<EventName>()`
- `SignalrService.on<EventName>()`
- `ScanSessionStore` — subscription and handler

### API backward compatibility
*Describe if the contract changes and how it affects OpenAPI client regeneration.*

---

## Contradictions with existing documentation

| # | What the requirements say | How it works today | Resolution |
|---|---|---|---|
| 1 | | | |

---

## Open questions

> Questions that remain unanswered — for discussion with the client.

- [ ] **Q1:** ...
- [ ] **Q2:** ...

---

## Out of scope

*What is explicitly NOT part of this task (to prevent scope creep).*

- ...

---

## Technical risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Schema change requires DB recreation | — | High | Schedule maintenance window |
| | | | |
