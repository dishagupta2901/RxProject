# Infrastructure adapter plan

Infrastructure will implement the Application ports using PostgreSQL/EF Core, Redis via StackExchange.Redis, Kafka via Confluent.Kafka against Redpanda, and typed `HttpClient` connectors for pricing, lab capability/load, coating, and shipment fakes. Connector timeout budgets belong to typed clients; Hangfire owns job retries and application services enforce idempotency.

`EventEnvelope` is the initial versioned event shape. Final topic names, outbox dispatch, telemetry exporters, and package versions remain governed by `DECISIONS.md` and will be added only after restore is available.
