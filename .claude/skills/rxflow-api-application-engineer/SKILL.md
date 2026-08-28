---
name: rxflow-api-application-engineer
description: Build RxFlow's HTTP boundary and application/use-case layer — request/response contracts, authentication/authorization wiring, validation, and orchestration of domain services through ports.
---

Use for a change that touches `src/RxFlow.Api`, `src/RxFlow.Application`, or their tests. Read `Agents.md`, `Requirements.md`, `architecture.md`, and `DECISIONS.md` first.

## Scope

- Owns `src/RxFlow.Api` (public HTTP boundary, token authentication, endpoint contracts, transport-level validation, request tracing) and `src/RxFlow.Application` (`SubmitOrder` and subsequent use cases, orchestration of domain services and repository/connector ports).
- `RxFlow.Application` coordinates domain services and ports **without knowing EF, Redis, Kafka, or Hangfire APIs** — infrastructure types stay out of this layer; depend on the ports defined here instead.
- Uses the accepted API style (controllers, per `DECISIONS.md` D-001) and validation approach (FluentValidation at the API/application boundary, per D-002); domain invariants stay in `RxFlow.Domain`.
- Implements the lab-override workflow and authentication/authorization policies using synthetic issuer/audience/role fixtures only.

## Boundaries

- Cross-boundary access goes through an explicit port or contract — never reach into `RxFlow.Infrastructure` or `RxFlow.Workers` internals.
- Domain and API/application work must agree on contracts before either adds infrastructure assumptions.
- Specify idempotency and failure semantics explicitly at the application boundary; no unbounded nested retries (retry/timeout ownership per D-003).
- `POST /orders` (and any later payment-equivalent submission endpoint) must reject or safely no-op a duplicate submission, backed by a test that submits the same request twice and asserts a single order/effect (Agents.md durable rule 1). This is currently unimplemented — `OrdersController` has no idempotency-key or dedupe check — so it is open work, not an established guarantee.
- A change to a public route, request/response schema, status code, or error contract requires a written compatibility note in the PR/change description (what breaks for existing callers, migration/versioning if anything) before merge (Agents.md durable rule 3). No compatibility-review process exists yet in this repo; establish it the first time a public contract changes.

## Working rules

- Add application tests using the single selected mocking library (see `DECISIONS.md`); avoid timing-only concurrency tests (`Thread.Sleep`/`Task.Delay`) — use deterministic coordination.
- Map domain failures to stable, documented HTTP contracts.
- Use synthetic identifiers and fixtures only.
- Update `Requirements.md`, `architecture.md`, and `DECISIONS.md` when an assumption becomes a decision.
- Run `dotnet build`/`dotnet test` for the affected projects and report actual commands, exit codes, and output.

## Escalate to the architect when

A change introduces a second framework/validation choice, crosses a planned boundary, changes retry/idempotency ownership, or adds a real external dependency.
