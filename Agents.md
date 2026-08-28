# RxFlow agent instructions

## Purpose and scope

RxFlow is a deliberately small .NET teaching repository for a corporate training lab. It models a synthetic prescription-to-lens order: an optician submits a prescription and frame choice, the system validates and prices the order, routes work to an Rx lab, schedules surfacing/coating, and tracks shipment. It is a training artefact, not a production system. Never connect it to real patient, prescription, corporate, production, or shared-infrastructure data.

This document governs future implementation work. At this stage the repository contains documentation only; the structure below is a plan, not a request to create directories now.

## Source of truth and boundaries

## Preparation update (2026-08-28)

Repository analysis established the source, frontend, test, database, deployment, and docs directories listed below.

## Governance update (2026-08-28)

The directories above are no longer empty: `src/RxFlow.Api`, `RxFlow.Application`, `RxFlow.Domain`, `RxFlow.Infrastructure`, `RxFlow.Workers`, and `RxFlow.Reporting` contain real implementation code, `database/migrations` has real EF Core migrations, and `frontend/` has a working React/TypeScript client. The "intentionally empty" / "planned layout" framing below is stale for those paths; treat the tree as the actual current layout, not a future plan. `Requirements.md`, `architecture.md`, and `DECISIONS.md` still carry some now-inaccurate "no implementation yet" status lines — that mismatch is a known limitation to resolve via `rxflow-decision-sync`, not something this update rewrites. See "Durable governance rules" below for the eight repository-wide rules every change must satisfy regardless of which skill or directory owns it.

- The supplied project brief is the current product and lab specification.
- Known facts, proposed decisions, assumptions, and open questions must be labelled as such in design documentation.
- Keep participant-facing material free of instructor answer keys, defect locations, and hints about intentionally seeded defects.
- Keep instructor-only deliverables outside the participant repository in a sibling `instructor` directory when implementation begins.
- Use synthetic identifiers and fixtures exclusively.
- Do not add dependencies, generated files, migrations, Docker assets, or source code until implementation is explicitly started.

## Planned folder structure

The following is the intended layout for a later implementation. Do not create it as part of the documentation phase.

    /src
      /RxFlow.Api                 # HTTP boundary, authentication, request/response contracts
      /RxFlow.Application         # use cases, orchestration, ports, validation coordination
      /RxFlow.Domain              # order, prescription, lens, lab and shipment rules
      /RxFlow.Infrastructure      # EF Core, PostgreSQL, Redis, Kafka, HTTP connectors
      /RxFlow.Workers             # Hangfire jobs and retry/dispatch configuration
      /RxFlow.Reporting           # reporting/read-model boundary used by the lab exercise
    /tests
      /RxFlow.Api.Tests
      /RxFlow.Application.Tests
      /RxFlow.Domain.Tests
      /RxFlow.Infrastructure.Tests
      /RxFlow.IntegrationTests
      /RxFlow.PropertyTests
    /database
      /migrations                  # EF Core migration artifacts (four to six planned)
    /deploy
      docker-compose.yml           # isolated local PostgreSQL, Redis, Redpanda, Hangfire support
    /docs
      architecture.md
      Requirements.md
      DECISIONS.md
    global.json
    Directory.Build.props
    Directory.Packages.props
    RxFlow.sln
    README.md

Instructor-only files must live beside, not inside, the participant tree:

    ../instructor/INSTRUCTOR.md
    ../instructor/INSTRUCTOR-DIAGRAMS.md

## Stack and consistency rules

- Pin the .NET 8 SDK in `global.json`; centralize package versions.
### FRONTEND / BACKEND

- **Frontend:** React with TypeScript.
- **Backend:** .NET 8 / C# 12 / ASP.NET Core Web API.
- Keep frontend and backend as clearly separated areas of the repository with an explicit boundary between them.
- React communicates with the ASP.NET Core backend through HTTP APIs.
- The backend is the source of truth for domain rules, validation, pricing, routing, authorization, persistence, and order state.
- Do not move business/domain logic into the React frontend for convenience.
- Choose the React project structure, routing, state management, API client, and UI libraries based on the project requirements and teaching goals rather than imposing a generic template.
- Keep frontend dependencies appropriately pinned and managed.
- Include the frontend in the repository's development and Docker Compose workflow where appropriate.
- Frontend build, lint/format, and test commands must actually run successfully.
- The `POST /orders` workflow should be traceable from the React frontend/API client through the ASP.NET Core backend and into the downstream processing flow described in the requirements.
- Keep the frontend realistic but intentionally simple; this is a **training repository, not a production product**.
- Use C# 12, nullable reference types, .NET analyzers with warnings as errors, `dotnet format`, and package vulnerability checks.
- Choose one HTTP style (controllers or minimal APIs) and one validation approach (DataAnnotations or FluentValidation) for the whole repository. Record the choice in `DECISIONS.md` before coding.
- Keep a plain C# application/domain service layer between the API and infrastructure.
- Use PostgreSQL with EF Core/Npgsql and migrations; Redis via StackExchange.Redis for cache and lock concerns; Hangfire for workers; Kafka through Redpanda and Confluent.Kafka; OpenTelemetry for traces, metrics, and logs.
- Use xUnit, FluentAssertions, one mocking library (NSubstitute or Moq), FsCheck, and Testcontainers for .NET. Do not mix alternatives without a recorded reason.
- Preserve real project/module boundaries. Cross-boundary access goes through an explicit port or contract; any lab-required boundary violation must remain ordinary-looking production code and must not be documented for participants.
- Avoid timing-only concurrency tests (`Thread.Sleep`/`Task.Delay`). Tests must use deterministic coordination and realistic work.

## Durable governance rules

These are repository standards, not one-time task instructions. They apply everywhere in the repo regardless of which `.codex/skills/*` file or directory a change touches. A skill file may add component-specific detail; none may weaken these.

1. **Order-submission idempotency.** `POST /orders` — and any future payment-equivalent, state-committing submission path — must be idempotent and covered by a duplicate-submission test. Ownership (application code owns idempotency, Hangfire owns job retries) is D-003 in `DECISIONS.md`, currently **proposed, not yet implemented or tested** — see `.codex/skills/rxflow-api-application-engineer/SKILL.md` and `.codex/skills/rxflow-worker-reliability-engineer/SKILL.md`.
2. **Database migrations.** Every EF Core migration documents upgrade behavior and rollback-or-forward-fix behavior, and is validated by an apply/downgrade integration test. See `.codex/skills/rxflow-infrastructure-engineer/SKILL.md` and `Requirements.md`.
3. **Public API compatibility.** A change to a public HTTP route, request/response schema, status code, or error contract requires an explicit compatibility review (what breaks existing callers, how it's communicated) before merge. See `.codex/skills/rxflow-api-application-engineer/SKILL.md`.
4. **New external calls.** Any new outbound HTTP/Kafka/Redis call states its timeout and retry policy, including whether the call is safe to retry, before or alongside the code that adds it (D-003). A `ConnectorOptions.Timeout` that is defined but never applied to the `HttpClient`, or a retry policy that is claimed but untested, does not satisfy this rule.
5. **Sensitive data must never be logged.** Prescription measurements, lens/frame specifics tied to a person, and auth tokens are sensitive (D-006, `DECISIONS.md`); logs, traces, and Kafka events may carry only synthetic order IDs and status. No nested skill or component file may narrow this rule.
6. **Infrastructure changes require security validation.** Changes to Compose services, connector configuration, secrets handling, or authentication (D-007) are reviewed for least privilege, no real credentials or endpoints, and no locally-exposed surface beyond what the lab needs.
7. **Defect fixes require a regression test.** Every fix for a reported or discovered defect adds a test that reproducibly fails before the fix and passes after it; capture that before/after evidence in the final report. This does not relax `rxflow-verification-engineer`'s existing rule against loosening assertions to hide seeded training defects.
8. **Final reports show evidence.** Every reported outcome states the exact command, working directory, exit code, and a concise result — never "should work" without having run it. (Restated by Workflow step 4 below and by most `.codex/skills/*` files; this is the one universal rule every skill is expected to repeat rather than merely link to, since it governs how *all* other evidence is reported.)

## Workflow

1. Read `Requirements.md`, `architecture.md`, and `DECISIONS.md` before changing design or code.
2. Update documentation when a proposal becomes a decision, an assumption changes, or an open question is resolved.
3. Make the smallest change that preserves the lab's 90-minute tracing exercise and isolated local stack.
4. Run restore, build, test, format verification, analyzers, vulnerability checks, and relevant integration/reproduction commands. Report actual commands, exit codes, elapsed time, and relevant output.
5. Review logs and test fixtures for accidental real-world identifiers or secrets.
6. Keep participant README plausible and maintainable; instructor evidence belongs only in the sibling instructor directory.

## Done criteria for future implementation

The acceptance conditions in the supplied brief are binding: clean local Compose startup, pinned restore, warning-free build, passing tests, format/analyzer/vulnerability checks, working migrations, end-to-end `POST /orders`, reproducible instructor demonstrations, and renderable diagrams. A change is not done when it merely compiles.
