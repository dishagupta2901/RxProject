# RxFlow phased implementation plan

This plan starts after the current documentation/preparation phase. Each phase should leave the repository buildable and locally testable; do not begin the next phase while its acceptance checks are failing.

## Phase 0 — Lock design decisions and tooling

1. Resolve and record every pending item in `DECISIONS.md`: API style, validation, retry/timeout ownership, Kafka consistency, event contracts, redaction, auth fixtures, dependency versions, and frontend toolchain.
2. Pin the .NET 8 SDK and central package versions; select the React package manager and Node version policy.
3. Create the solution/project files, shared build props, formatting/analyzer settings, `.gitignore`, and baseline CI-quality commands.
4. Define synthetic fixture conventions, correlation identifiers, and public API/error contracts.

**Exit checks:** pinned restore succeeds; empty projects compile; format and analyzers pass.

## Phase 1 — Domain kernel (started 2026-08-28)

1. Implement value objects and entities for prescription, lens/frame selection, order, lab capability/load, production scheduling, and shipment.
2. Encode physical grindability, lifecycle transitions, pricing inputs, and invariant failures in plain C# with no infrastructure references.
3. Add unit and FsCheck property tests for boundary values and state transitions.

**Exit checks:** domain tests and property tests pass; dependency direction is domain-only.

## Phase 2 — Application use cases and ports (started 2026-08-28)

1. Define `SubmitOrder`, validation/pricing, lab-routing, scheduling, shipment, and lab-override use cases.
2. Define explicit ports for repositories, clock/id generation, connector calls, job dispatch, cache/locks, and event publication.
3. Specify idempotency and failure semantics at the application boundary.
4. Add application tests with the single selected mocking library.

**Exit checks:** use cases run entirely against in-memory fakes; contracts are stable enough for API and infrastructure work.

## Phase 3 — API and authentication slice (started 2026-08-28)

1. Implement the chosen controllers/minimal APIs and validation approach.
2. Add token authentication, authorization policies, and the lab-override workflow using synthetic issuer/audience/role fixtures.
3. Map requests/responses and domain failures to stable HTTP contracts.
4. Add request correlation and initial OpenTelemetry instrumentation.

**Exit checks:** authenticated `POST /orders` succeeds and invalid requests produce documented responses; API tests pass.

## Phase 4 — Persistence and migrations (started 2026-08-28)

1. Implement EF Core/Npgsql mappings and repositories for authoritative workflow state.
2. Create four to six purposeful migrations, seed only synthetic data, and document downgrade behavior.
3. Add Testcontainers PostgreSQL integration tests for persistence, concurrency constraints, and migration apply/downgrade.

**Exit checks:** clean database migration and downgrade both work; repository tests pass without timing sleeps.

## Phase 5 — Infrastructure adapters and local dependencies (started 2026-08-28)

1. Implement Redis cache and distributed-lock adapters with observable ownership/expiry failures.
2. Implement typed/named `HttpClient` connectors for pricing, lab capability/load, coating, and shipment fakes.
3. Implement Kafka/Redpanda publishing using the accepted event schema and consistency strategy.
4. Complete telemetry for database, Redis, connectors, messaging, and worker boundaries.

**Exit checks:** adapter integration tests pass against ephemeral containers or deterministic local fakes; no production endpoints are configurable by default.

## Phase 6 — Workers and end-to-end order flow (started 2026-08-28)

1. Implement Hangfire jobs for lab submission, surfacing/coating scheduling, status progression, and shipment updates.
2. Add Kafka consumers in Workers if required by the accepted design; enforce idempotency and retry limits.
3. Propagate trace/correlation context across API → Hangfire → connectors/Kafka → consumers.
4. Add end-to-end tests covering `POST /orders` through shipment state.

**Exit checks:** the complete synthetic flow is repeatable and observable; worker retries and duplicate delivery are deterministic.

## Phase 7 — Reporting boundary and React client (started 2026-08-28)

1. Implement read-only reporting queries and contracts without exposing write-model persistence internals.
2. Build the intentionally small React/TypeScript client: order form, validation feedback, status view, and lab-override entry point.
3. Keep API types and client behavior aligned through contract tests or generated types selected in Phase 0.

**Exit checks:** frontend build, lint/format, and tests pass; the client can submit and trace an order against the local API.

## Phase 8 — Compose, scenarios, and acceptance evidence (started 2026-08-28)

1. Add isolated Docker Compose services for API, frontend, PostgreSQL, Redis, Redpanda, Hangfire support, and connector fakes.
2. Add health/readiness checks and a clean-start smoke test.
3. Obtain the missing ten-scenario inventory, implement synthetic fixtures, and reproduce each scenario three times with deterministic coordination.
4. Run quality gates, migration checks, vulnerability checks, and end-to-end demonstrations; capture commands, exit codes, timings, and telemetry evidence.
5. Keep instructor explanations and exactly two Mermaid diagrams in the sibling `../instructor` directory.

**Exit checks:** all acceptance criteria in `Requirements.md` are evidenced, not inferred; participant documentation contains no answer keys or defect hints.

## Cross-phase working rules

- Prefer vertical slices and small changesets over broad scaffolding.
- Any new cross-boundary dependency requires an explicit port/contract and a decision record.
- Update `Requirements.md`, `architecture.md`, and `DECISIONS.md` when assumptions become decisions.
- Never use real personal, patient, corporate, production, or shared-infrastructure data.
