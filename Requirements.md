# RxFlow requirements

## Status and evidence labels

The repository is empty. “Known” items come directly from the supplied brief; “proposed” items are architectural interpretations; “open” items need an explicit decision before implementation.

## Functional requirements

Known:

1. Accept an optician order with a prescription and frame choice through `POST /orders`.
2. Validate whether the prescription can be physically ground into the selected lens.
3. Price the order.
4. Route work to an Rx lab using capability and live-load information.
5. Schedule surfacing and coating.
6. Track the order through shipment.
7. Protect most endpoints with token authentication and expose a lab-override workflow.
8. Persist workflow state with EF Core/PostgreSQL, process asynchronous work with Hangfire, cache/coordinate with Redis, and publish Kafka events through Redpanda.
9. Provide three or four outbound HTTP integrations through `HttpClient`/`IHttpClientFactory`.
10. Provide reproducible local demonstrations and instructor evidence for the ten seeded training scenarios described in the source brief, while keeping the participant test suite green.

## Non-functional requirements

- .NET 8, C# 12, pinned SDK, centralized package versions, nullable reference types, analyzers as errors, `dotnet format`, and vulnerability checks.
- One consistent API style, validation framework, mocking framework, and background-job framework choice.
- Docker Compose must start an isolated local stack containing PostgreSQL, Redis, Redpanda/Kafka, and Hangfire support.
- EF Core must apply four to six migrations to a clean database; downgrade behavior must be exercised.
- Tests use xUnit, FluentAssertions, FsCheck, and Testcontainers for .NET as appropriate.
- OpenTelemetry traces, metrics, and logs must cover the end-to-end request path.
- Concurrency tests must use realistic work and deterministic coordination, never timing-only sleeps.
- The main path should be traceable within a 90-minute lab; project boundaries must be genuine and findable.
- No real personal, patient, prescription, corporate, production, or shared-infrastructure data.

## Constraints

- Participant repository must not contain instructor answer keys, defect maps, or explanatory hints.
- Instructor guide and exactly two Mermaid diagrams live in a sibling instructor directory.
- The README is intentionally plausible but may become partly stale as part of the exercise; acceptance evidence must distinguish commands actually run from claims inferred from docs.
- Reproduction evidence must be repeatable three times without delay-based synchronization.

## Assumptions (to validate)

- A single local Compose project and synthetic fixtures are sufficient for all training exercises.
- Connector fakes can model capability, pricing, coating, and shipment interactions without external network access.
- A bearer-token test issuer or static local token mechanism is adequate for local authentication exercises.
- The first implementation can use one deployable API plus workers while retaining project boundaries in the solution.

## Open questions

- The source brief's ten seeded training scenarios and their fixture contracts are not present in this repository; obtain or inventory them before implementation/evidence work.
- The required React/TypeScript client location and build/test toolchain must be selected and recorded.

- Should the API use controllers or minimal APIs, and should validation use DataAnnotations or FluentValidation?
- Which layer owns retries and timeout budgets across callers, Hangfire workers, and connectors? How are policies composed and observed?
- Is an outbox required for the PostgreSQL-to-Kafka handoff, or is at-least-once publication sufficient for this lab?
- What exact order/event schema and retention policy should local Redpanda topics use?
- Which fields are classified as sensitive, and what is the approved redaction policy for logs, traces, serialized records, and diagnostics?
- What token issuer/audience and lab-override authorization role should local fixtures represent?
- Which database provider/version and vulnerability threshold are supported by the training environment?
