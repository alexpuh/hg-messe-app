# Requirements Analyst — GitHub Copilot Space Setup

A specialized agent for analyzing, clarifying, and formalizing technical requirements.

## What the agent does

1. Accepts raw requirements text (incomplete or contradictory)
2. Compares against technical documentation in `tech-doc/`
3. Identifies contradictions, gaps, and ambiguities
4. Asks clarifying questions
5. Creates a structured MD task file in `docs/tasks/` (for client discussion and transfer to Jira)

**Output is an MD file only — no code changes.**

---

## Option A: GitHub Copilot Space (recommended)

### Creating the Space

1. Go to [github.com/copilot](https://github.com/copilot) → **Spaces** → **New space**
2. Name: `Requirements Analyst — Messe App`
3. Connect repository `alexpuh/hg-messe-app` as context source
4. In the **System prompt** field, paste the contents of `.github/instructions/tz-analyst.instructions.md`
5. Add the following files as context:
   - `tech-doc/architecture.md`
   - `docs/tz-output-template.md`

### Usage

Open the Space and paste the requirements text. The agent will analyze it, ask questions, and create a task file.

---

## Option B: Copilot CLI (terminal)

The file `.github/instructions/tz-analyst.instructions.md` is automatically picked up by Copilot CLI.

Activate:
```
/instructions
```
Select `tz-analyst` → instructions will be applied to the current session.

Then paste the requirements text into the chat.

---

## File structure

```
.github/
  instructions/
    tz-analyst.instructions.md   ← agent instructions
tech-doc/
  architecture.md                ← technical documentation (source of truth)
docs/
  tz-output-template.md          ← output MD template
  tasks/
    README.md
    YYYY-MM-DD_<slug>.md         ← created by the agent
```

---

## Output file template

The agent uses `docs/tz-output-template.md`. Each task file contains:

- Summary and background
- Affected components table (by architecture layer)
- User stories
- Acceptance criteria (Gherkin format)
- Preliminary implementation notes
- Contradictions with existing documentation
- Open questions for the client
- Technical risks
