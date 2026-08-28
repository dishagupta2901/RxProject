# Planned subagents and collaboration

This is a coordination plan for later implementation. Each role below now has a corresponding skill under `.codex/skills/` (added 2026-08-28, moved from a short-lived `.claude/agents/` location the same day so Codex can load them too): `rxflow-domain-modeller`, `rxflow-api-application-engineer`, `rxflow-infrastructure-engineer`, `rxflow-worker-reliability-engineer`, `rxflow-verification-engineer`, `rxflow-instructor-doc-editor`, and `rxflow-frontend-contract-engineer`. Invoking a role does not itself authorize skipping phase gates in `docs/implementation-plan.md`.

## Roles

| Role | Responsibility | Primary outputs |
|---|---|---|
| Domain modeller | Prescription, lens, frame, order, lab capability, scheduling and shipment concepts; invariants and examples | `src/RxFlow.Domain`, domain tests |
| API/application engineer | HTTP boundary, request validation, authentication/authorization wiring, use-case orchestration and ports | `src/RxFlow.Api`, `src/RxFlow.Application`, API/application tests |
| Infrastructure engineer | EF Core/PostgreSQL, Redis, Kafka/Redpanda, outbound HTTP clients, OpenTelemetry and configuration | `src/RxFlow.Infrastructure`, migrations, integration tests |
| Worker/reliability engineer | Hangfire jobs, scheduling, retry policies, locks/counters, operational diagnostics | `src/RxFlow.Workers`, reliability tests |
| Verification engineer | Testcontainers scenarios, property tests, quality gates, Compose smoke tests and acceptance evidence | `tests/*`, command evidence |
| Instructor-documentation editor | Participant README and design docs; separate instructor guide and exactly two diagrams | `docs/*`, sibling `../instructor/*` |
| Reporting/analytics *(gap — no skill yet)* | `src/RxFlow.Reporting` exists in the repo (`OrderReportingService`, `OrderStatusView`, wired to `ReportsController`) but has no dedicated `.codex/skills/*` role; it currently only inherits the root `Agents.md` durable rules. Create `rxflow-reporting-engineer` (or fold into `rxflow-api-application-engineer`) before making non-trivial changes there, covering parameterized queries only, de-identified/aggregate fields only, and schema-compatibility review. | `src/RxFlow.Reporting` — unowned |

## Collaboration protocol

- The architect/lead owns cross-cutting decisions, project boundaries, and the integration branch.
- Domain and API/application work should agree on contracts before either adds infrastructure assumptions.
- Infrastructure and worker work should share explicit ports and configuration names; neither should reach into another project's persistence internals.
- Verification work starts with contract-level tests and then exercises the isolated Compose stack. It must not “fix” or remove training scenarios without an explicit decision.
- The documentation editor records decisions and unresolved questions, but never copies instructor findings into participant-facing docs.
- Each handoff includes changed files, assumptions, commands run, and known follow-up risks. Prefer small, reviewable commits/changesets.
- When two roles need the same contract, define it in the owning boundary and review the dependency direction rather than duplicating types.

## Preparation update (2026-08-28)

Add a **Frontend/API contract engineer** role owning the React client, API-client typing, and end-to-end traceability without duplicating backend rules. Temporary delegated reviews during repository analysis covered requirements, architecture, and project-specific workflow skills.

## Skills update (2026-08-28)

`.codex/skills/` now covers both the workflow gates in `Agents.md` and the per-role playbooks above, all in the one location Codex actually reads: `rxflow-quality-gates` and `rxflow-reproduction-evidence` already existed (restore/build/test/format/analyzer/vulnerability checks and deterministic scenario reproduction); `rxflow-decision-sync` (keep `Requirements.md`/`architecture.md`/`DECISIONS.md` synchronized) and `rxflow-participant-boundary` (screen for real-world identifiers, secrets, and instructor-only content before a change is done) closed the workflow-step gaps; and the seven role skills (`rxflow-domain-modeller` through `rxflow-frontend-contract-engineer`) give Codex the same scope/boundary/escalation guidance a human in that role would follow. `rxflow-diagram-sync` is a further cross-cutting addition (not one of the seven module roles): it keeps `docs/diagrams.md` synchronized with the code across all layers whenever a change adds, removes, or reroutes a call path, the same way `rxflow-decision-sync` keeps the decision docs synchronized.

## Escalation points

Escalate to the architect when a change introduces a second framework choice, crosses a planned boundary, changes retry/idempotency ownership, adds a real external dependency, or makes the local stack non-deterministic. Escalate to the instructor-documentation editor when behavior needed for a reproduction is not observable without exposing participant hints.
