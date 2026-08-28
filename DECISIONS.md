# Architecture decisions

This record starts empty by design. The repository has no implementation yet, so entries below capture decisions that must be made before coding rather than inventing facts.

## Decision template

- **ID / date:**
- **Status:** proposed | accepted | superseded
- **Decision:**
- **Context:**
- **Alternatives considered:**
- **Consequences:**
- **Evidence or follow-up:**

## Pending decisions

1. API style: controllers or minimal APIs.
2. Validation: DataAnnotations or FluentValidation.
3. Retry and timeout ownership across API callers, workers, and connectors.
4. PostgreSQL-to-Kafka consistency strategy (outbox or documented at-least-once handoff).
5. Event naming/schema and local topic retention.
6. Sensitive-data classification and telemetry redaction.
7. Local authentication issuer, audience, token fixture, and lab-override role.
8. Supported dependency versions and vulnerability policy threshold.

Every accepted choice should be added here before the implementation that depends on it, with the date and consequences recorded.

## Phase 0 decisions (2026-08-28)

- **D-001 — API style:** accepted: controllers. They provide explicit HTTP contracts and are familiar for the training lab.
- **D-002 — Validation:** accepted: FluentValidation at the API/application boundary. Domain invariants remain plain C# rules.
- **D-003 — Retries/timeouts:** proposed pending implementation validation: connector timeout budgets live in typed clients; Hangfire owns job retries; application code owns idempotency. No unbounded nested retries.
- **D-004 — Kafka consistency:** proposed: transactional outbox in PostgreSQL, with Workers publishing and marking records dispatched.
- **D-005 — Events:** proposed: versioned `rxflow.order.v1` topic and `OrderStatusChanged.v1` envelope; local retention is development-only and documented in Compose.
- **D-006 — Telemetry redaction:** proposed: prescription measurements and tokens are sensitive; logs/traces/events contain synthetic order IDs and status only.
- **D-007 — Authentication:** proposed: local JWT bearer issuer/audience fixtures; `lab-override` role required for override operations.
- **D-008 — Toolchain:** accepted: .NET 8 SDK pinned via `global.json`; React toolchain uses a pinned Node/npm version once the environment provides Node. Current host has .NET 10 only and no Node/npm, so restore/build verification is blocked until tooling is installed.
- **D-009 — SDK override (2026-08-28):** accepted superseding D-008 at the user's request: pin the currently installed .NET 10.0.400 SDK so C# Dev Kit can load the repository. This is a temporary deviation from the supplied .NET 8 requirement and must be revisited before training acceptance.
- **D-010 — Package compatibility (2026-08-28):** accepted: use EF Core 9.0.4 with Npgsql EF provider 9.0.4 on the .NET 10 SDK because no stable Npgsql EF 10 package is available from the configured feed. Revisit if the project returns to .NET 8 or a compatible provider becomes available.
