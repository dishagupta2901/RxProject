---
name: rxflow-diagram-sync
description: Keep docs/diagrams.md (repo structure, code/request flow, API/integration surface) synchronized with the code as it actually exists, whenever a change adds, removes, or rewires a call path across RxFlow.Api, Application, Domain, Infrastructure, Workers, Reporting, or the frontend.
---

Use whenever a change adds, removes, or reroutes a call path across module boundaries — a new use case, a dispatcher/repository registration change, a new outbound connector actually being invoked, a new Hangfire job, a new Kafka producer/consumer, a new HTTP endpoint, or a frontend call into a different API route. This is a cross-cutting skill, not owned by any single `src/*` project: a code-flow diagram is only correct if it is drawn from what every layer actually does, so read across `RxFlow.Api`, `RxFlow.Application`, `RxFlow.Domain`, `RxFlow.Infrastructure`, `RxFlow.Workers`, `RxFlow.Reporting`, and `frontend/` rather than from one module's perspective.

## Rules

- `docs/diagrams.md` must describe the code as it exists today, not the planned/future state in `Agents.md` or `docs/implementation-plan.md`. If a wire-up is registered in DI but never actually invoked (as several connectors currently are — see the existing notes under diagram 3), the diagram and its notes must say so explicitly rather than implying the call happens.
- Update all three diagrams together when a change touches more than one: repo/project-reference structure, the sequence diagram for request/job flow, and the API/integration surface. A change that only affects one diagram still needs the other two checked for staleness.
- Re-date the top of the file to the date of the change and keep the note that it matches `docs/acceptance-evidence.md`; if it no longer matches, fix or flag the mismatch rather than leaving a false claim.
- No seeded-defect locations, answer keys, or instructor hints belong in these diagrams — they are participant-facing (`rxflow-participant-boundary` applies here too). Instructor-only diagram variants belong in the sibling `../instructor/INSTRUCTOR-DIAGRAMS.md`, never in `docs/diagrams.md`.
- Verify every mermaid block actually renders (no syntax the mermaid version in use rejects) before calling the update done.
- When a diagram claim depends on a specific file/line (e.g. which repository/dispatcher implementation DI resolves to), cite it the way the existing notes do, so the diagram stays checkable against the code rather than becoming prose that quietly drifts.
