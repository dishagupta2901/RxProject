# Planned subagents and collaboration

This is a coordination plan for later implementation. No subagents are required for the documentation-only phase.

## Roles

| Role | Responsibility | Primary outputs |
|---|---|---|
| Domain modeller | Prescription, lens, frame, order, lab capability, scheduling and shipment concepts; invariants and examples | `src/RxFlow.Domain`, domain tests |
| API/application engineer | HTTP boundary, request validation, authentication/authorization wiring, use-case orchestration and ports | `src/RxFlow.Api`, `src/RxFlow.Application`, API/application tests |
| Infrastructure engineer | EF Core/PostgreSQL, Redis, Kafka/Redpanda, outbound HTTP clients, OpenTelemetry and configuration | `src/RxFlow.Infrastructure`, migrations, integration tests |
| Worker/reliability engineer | Hangfire jobs, scheduling, retry policies, locks/counters, operational diagnostics | `src/RxFlow.Workers`, reliability tests |
| Verification engineer | Testcontainers scenarios, property tests, quality gates, Compose smoke tests and acceptance evidence | `tests/*`, command evidence |
| Instructor-documentation editor | Participant README and design docs; separate instructor guide and exactly two diagrams | `docs/*`, sibling `../instructor/*` |

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

## Escalation points

Escalate to the architect when a change introduces a second framework choice, crosses a planned boundary, changes retry/idempotency ownership, adds a real external dependency, or makes the local stack non-deterministic. Escalate to the instructor-documentation editor when behavior needed for a reproduction is not observable without exposing participant hints.
