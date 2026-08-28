---
name: rxflow-decision-sync
description: Keep Requirements.md, architecture.md, and DECISIONS.md synchronized whenever a proposal becomes a decision, an assumption changes, or an open question resolves.
---

Use whenever a change accepts a proposal, changes a stated assumption, or resolves an open question in `Requirements.md`, `architecture.md`, or `DECISIONS.md`. Update all three together in the same changeset: move the item out of "open questions"/"proposed", record it in `DECISIONS.md` using the existing ID/date/status/decision/context/alternatives/consequences/evidence template, and reflect the resulting behavior in `architecture.md`. Never record a decision that was not actually made, and never leave a decision recorded in one file but silent in the others.
