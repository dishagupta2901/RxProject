---
name: rxflow-domain-modeller
description: Model RxFlow's domain kernel — prescription, lens/frame selection, order lifecycle, lab capability/load, production scheduling, and shipment concepts, with their invariants and boundary examples.
---

Use for RxFlow domain-kernel work: a change that touches `src/RxFlow.Domain` or its tests, or a new domain rule/invariant that other layers need to depend on. Read `Agents.md`, `Requirements.md`, `architecture.md`, and `DECISIONS.md` first.

## Scope

- Owns `src/RxFlow.Domain` and its tests: prescription, lens/frame selection, order lifecycle, lab capability/load, production scheduling, shipment.
- Encodes physical grindability, lifecycle transitions, pricing inputs, and invariant failures in plain C# with **no infrastructure references** (no EF, Redis, Kafka, Hangfire, or HTTP types in this project).
- Adds unit and FsCheck property tests for boundary values and state transitions.

## Boundaries

- Dependency direction is domain-only: nothing outside `RxFlow.Domain` may be referenced from it.
- Do not reach into `RxFlow.Application` ports, persistence, or connector internals — if application/API work needs a new domain concept, define it here first and let the other boundary consume it.
- Domain and API/application work must agree on contracts before either adds infrastructure assumptions (per `subagents.md`).

## Working rules

- Use synthetic identifiers and fixtures only — never real patient, prescription, or corporate data.
- Make the smallest change that keeps the domain kernel buildable and testable; prefer small, reviewable changesets.
- Update `Requirements.md`, `architecture.md`, and `DECISIONS.md` when an assumption becomes a decision or an open question resolves.
- Run `dotnet build`/`dotnet test` for the affected projects and report actual commands, exit codes, and output — never claim a test passed without executing it.

## Escalate to the architect when

A change introduces a second framework choice, crosses a planned project boundary, changes retry/idempotency ownership, or makes the local stack non-deterministic.
