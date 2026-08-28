---
name: rxflow-infrastructure-engineer
description: Build RxFlow's infrastructure adapters — EF Core/PostgreSQL persistence and migrations, Redis cache/locks, Kafka/Redpanda publishing, outbound HTTP connectors, and OpenTelemetry wiring.
---

Use for a change that touches `src/RxFlow.Infrastructure`, database migrations, or their integration tests. Read `Agents.md`, `Requirements.md`, `architecture.md`, and `DECISIONS.md` first.

## Scope

- Owns `src/RxFlow.Infrastructure`: EF Core/Npgsql persistence and repositories, Redis cache and distributed-lock adapters, Kafka producer via Confluent.Kafka/Redpanda, typed/named outbound `HttpClient` connectors (`IHttpClientFactory`) for pricing, lab capability/load, coating, and shipment fakes, and OpenTelemetry exporters.
- Creates purposeful EF Core migrations (four to six planned, per `Requirements.md`), seeds only synthetic data, and documents downgrade behavior.
- Connector base URLs, credentials, and timeouts come from configuration and point only at local Compose fakes — never a real endpoint.

## Boundaries

- PostgreSQL is authoritative; cache entries are disposable and must never be the only copy of an order.
- Kafka events are integration notifications, not a replacement for transactional state (outbox strategy per D-004).
- Infrastructure and worker work must share explicit ports and configuration names; do not reach into `RxFlow.Workers`' internals or vice versa.
- Do not expose persistence/connector internals to `RxFlow.Reporting` or the API — those consume ports, not EF/Redis/Kafka types directly.

## Working rules

- Add Testcontainers PostgreSQL integration tests for persistence, concurrency constraints, and migration apply/downgrade — deterministic coordination only, no timing sleeps.
- Redis lock ownership, expiry, and failure behavior must be observable.
- Redpanda, Redis, PostgreSQL, and Hangfire storage stay isolated per checkout; no production SaaS, patient data, or shared topics/databases.
- Update `Requirements.md`, `architecture.md`, and `DECISIONS.md` when an assumption becomes a decision.
- Run `dotnet build`/`dotnet test` (and Compose-backed integration tests where applicable) and report actual commands, exit codes, and output.

## Escalate to the architect when

A change introduces a second framework choice, crosses a planned boundary, changes retry/idempotency ownership, adds a real external dependency, or makes the local stack non-deterministic.
