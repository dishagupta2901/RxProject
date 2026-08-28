# RxFlow architecture

## Status

This is a proposed architecture based on the supplied lab brief. The repository is currently empty, so there is no existing implementation to describe. Decisions marked “proposed” must be confirmed in `DECISIONS.md` before coding.

See `docs/diagrams.md` for the current repo-structure, code-flow, and API/integration-surface diagrams (kept in sync with the code, not aspirational).

## System context

An optician-facing client submits an order containing a prescription and frame choice. RxFlow validates physical grindability, calculates price, chooses an Rx lab, schedules surfacing and coating, and publishes progress toward shipment. All local dependencies are synthetic and run in isolated Docker Compose services.

## Components and responsibilities

- **RxFlow.Api** — public HTTP boundary, token authentication, endpoint contracts, transport-level validation, and request tracing.
- **RxFlow.Application** — `SubmitOrder` and subsequent use cases; coordinates domain services and repository/connector ports without knowing EF, Redis, Kafka, or Hangfire APIs.
- **RxFlow.Domain** — order lifecycle, prescription and lens rules, pricing policy, lab capability matching, and scheduling decisions.
- **RxFlow.Infrastructure** — EF Core/Npgsql persistence, Redis cache and lock adapters, Kafka producer, outbound `HttpClient` connectors, and telemetry exporters.
- **RxFlow.Workers** — Hangfire job handlers that perform asynchronous lab submission, scheduling, status updates, and event publication.
- **RxFlow.Reporting** — deliberately separate read/reporting boundary for operational queries; it must not become a back door into application write models.
- **PostgreSQL** — source of record for orders, prescriptions, frames, labs, jobs, and shipment state.
- **Redis** — cache and distributed-lock storage for short-lived coordination.
- **Redpanda/Kafka** — local event transport for order and lab workflow events.
- **External connector fakes** — local HTTP services standing in for pricing, lab-capability, coating, and shipment integrations.
- **OpenTelemetry** — traces, metrics, and logs across API, workers, persistence, cache, messaging, and connectors.

## Proposed request and event flow

`POST /orders` enters `RxFlow.Api`, binds an order request, and invokes the application submit-order service. The service validates and constructs domain values, persists the order through a repository, and enqueues a Hangfire job. The worker reloads the order, performs lab submission/scheduling through connector ports, and publishes a Kafka event. Consumers update order status and the shipment path. Correlation/trace context should cross the HTTP-to-worker-to-Kafka boundaries.

The exact endpoint style (controllers or minimal APIs), validation library, event schema, and job partitioning are open until recorded in `DECISIONS.md`.

## Data flow and consistency

PostgreSQL is authoritative. Cache entries are disposable and must never be the only copy of an order. Redis locks protect explicitly documented critical sections; lock ownership, expiry, and failure behavior must be observable. Kafka events are integration notifications, not a replacement for transactional state. The outbox/transaction approach is an open design question and should be decided before implementation.

## Integrations

All external calls use `IHttpClientFactory` and typed/named clients. Connector base URLs, credentials, and timeouts come from configuration and point only at local fakes in Compose. Redpanda, Redis, PostgreSQL, and Hangfire storage are isolated per checkout. No production SaaS, patient data, or shared topics/databases are allowed.

## Quality and operability

The solution will use nullable C#, analyzers-as-errors, format verification, vulnerability checks, unit/property tests, and Testcontainers integration tests. Health/readiness checks should cover PostgreSQL, Redis, Redpanda, and workers. Telemetry should include a stable order correlation identifier while applying a later-approved policy for sensitive-field redaction.

## Repository reconciliation (2026-08-28)

The requirements also mandate a React/TypeScript optician client. It will live under `frontend/` and communicate only through the API HTTP boundary; it does not own domain rules. Kafka consumers are initially hosted by `RxFlow.Workers`; a separate consumer deployable is not justified yet. The four outbound HTTP fakes are pricing, lab capability/load, coating, and shipment. Frontend toolchain and sync/async ownership for pricing remain open decisions.

## Important design tensions to resolve

The lab intentionally requires participants to reason about retry ownership, idempotency, live lab capacity, migration reversibility, concurrency, sensitive logging, authorization, and query safety. The implementation must remain realistic and testable while keeping those concerns visible through ordinary code paths. Instructor explanations and exact defect locations belong outside this repository.
