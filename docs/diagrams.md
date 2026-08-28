# RxFlow diagrams

Participant-facing architecture diagrams reflecting the code as it exists today (2026-08-28, matching `docs/acceptance-evidence.md`). These describe structure and control flow only — no seeded-defect locations or answer keys; those stay in the sibling `../instructor` directory per `Agents.md`.

## 1. Repository / project structure

Solid arrows are compile-time project references (`RxFlow.slnx`); the dashed arrow is a runtime HTTP call, not a project reference.

```mermaid
flowchart TB
    subgraph src [" src/ "]
        Api["RxFlow.Api<br/>(HTTP boundary, auth, composition root)"]
        Application["RxFlow.Application<br/>(use cases, ports)"]
        Domain["RxFlow.Domain<br/>(LensOrder, Prescription, Frame)"]
        Infrastructure["RxFlow.Infrastructure<br/>(EF Core, Redis, Kafka, HTTP connectors)"]
        Workers["RxFlow.Workers<br/>(Hangfire jobs)"]
        Reporting["RxFlow.Reporting<br/>(empty stub — no source yet)"]
    end

    subgraph tests [" tests/ "]
        DomainTests["RxFlow.Domain.Tests"]
        AppTests["RxFlow.Application.Tests"]
        InfraTests["RxFlow.Infrastructure.Tests"]
    end

    Frontend["frontend/<br/>(React + TS, Vite)"]
    Database["database/migrations<br/>(EF Core migrations)"]
    Deploy["deploy/docker-compose.yml"]

    Api --> Application
    Api --> Infrastructure
    Api --> Workers
    Application --> Domain
    Infrastructure --> Domain
    Infrastructure --> Application
    Workers --> Application
    Workers --> Domain
    Database -.->|generates| Infrastructure

    DomainTests --> Domain
    AppTests --> Application
    InfraTests --> Infrastructure

    Frontend -.->|HTTP: POST /orders| Api

    style Reporting stroke-dasharray: 5 5
```

**Notes**

- `RxFlow.Reporting` is declared in `RxFlow.slnx` and `architecture.md` but currently contains only a `.csproj` — no source files, no reporting test project exists yet (Phase 7 of `docs/implementation-plan.md` is not started).
- Dependency direction matches `subagents.md`'s collaboration protocol: `Domain` has no outgoing dependencies; `Application` depends only on `Domain`; `Infrastructure`/`Workers`/`Api` depend inward, never sideways into each other's internals.

## 2. Code / request flow

Two independent flows exist today: the synchronous `POST /orders` path, and the Hangfire-scheduled outbox dispatch. They are not yet connected end-to-end to Kafka consumers or lab/coating/shipment connectors (see notes).

```mermaid
sequenceDiagram
    participant FE as frontend (main.tsx)
    participant API as OrdersController
    participant SVC as SubmitOrderService
    participant DOM as LensOrder (Domain)
    participant PRICE as IPriceCalculator
    participant REPO as IOrderRepository
    participant OUTBOX as IOutboxWriter
    participant DISPATCH as IOrderWorkDispatcher
    participant HF as Hangfire Server
    participant JOB as OrderWorkflowJob

    FE->>API: POST /orders {sphere, cylinder, axis, frame...}
    API->>SVC: SubmitAsync(SubmitOrderCommand)
    SVC->>DOM: new LensOrder(id, prescription, frame)
    SVC->>PRICE: CalculateAsync(order)
    PRICE-->>SVC: price
    SVC->>REPO: AddAsync(order)
    SVC->>OUTBOX: AppendAsync("OrderSubmitted.v1", ...)
    SVC->>DISPATCH: DispatchAsync(orderId)
    DISPATCH->>HF: Enqueue<OrderWorkflowJob>
    API-->>FE: 202 Accepted {orderId, price, status}

    Note over HF,JOB: runs asynchronously, separate from the HTTP request
    HF->>JOB: ExecuteAsync(orderId)
    JOB->>REPO: GetAsync(orderId)
    JOB->>DOM: ValidateGrindability(maxAbsolutePower: 12)

    Note over HF: RecurringJob "rxflow-outbox" fires every minute (Cron.Minutely)
    HF->>OUTBOX: OutboxDispatchJob → OutboxDispatcher.DispatchBatchAsync
    OUTBOX->>OUTBOX: read undispatched RxFlowDbContext.OutboxMessages
    OUTBOX->>OUTBOX: KafkaEventPublisher.PublishAsync(EventEnvelope)
    OUTBOX->>OUTBOX: mark DispatchedAt, SaveChangesAsync
```

**Notes**

- `IOrderRepository` is bound to `LocalOrderRepository` (an in-memory `ConcurrentDictionary`, [src/RxFlow.Api/LocalAdapters.cs](../src/RxFlow.Api/LocalAdapters.cs)) in `Program.cs`; `EfOrderRepository` exists in `RxFlow.Infrastructure` but is not registered there today, so orders do not currently persist to PostgreSQL through the API composition root as configured.
- `IOrderWorkDispatcher` is registered twice in `Program.cs` — `LocalWorkDispatcher` first, then `HangfireOrderWorkDispatcher` — the later registration wins, so dispatch actually goes through Hangfire.
- No Kafka **consumer** exists yet in `RxFlow.Workers`; the outbox path only publishes. Nothing currently reads `rxflow.order.v1` back into order status.
- `OrderWorkflowJob` only validates grindability; it does not yet call lab-routing, scheduling, coating, or shipment — those use cases are not implemented (Phases 5/6 of `docs/implementation-plan.md`).

## 3. API / integration surface

```mermaid
flowchart LR
    Client["Client (frontend or bearer-token caller)"]

    subgraph API ["RxFlow.Api"]
        Health["GET /health<br/>(anonymous)"]
        Orders["POST /orders<br/>[Authorize]"]
        OrdersGet["GET /orders/{id}<br/>[Authorize]"]
        OrdersCancel["POST /orders/{id}/cancel<br/>[Authorize]"]
        HangfireUI["/hangfire<br/>(dashboard, no policy applied)"]
    end

    Client -->|JWT bearer, Authority/Audience from config| Orders
    Client -->|JWT bearer| OrdersGet
    Client -->|JWT bearer| OrdersCancel
    Client --> Health
    Client --> HangfireUI

    subgraph AuthZ ["Authorization policies"]
        LabOverridePolicy["LabOverride policy<br/>(RequireRole: lab-override)"]
    end
    AuthZ -.->|defined but not applied to any endpoint yet| API

    subgraph Connectors ["Outbound HttpClient connectors (RxFlow.Infrastructure) — registered, not yet called by any use case"]
        Pricing["IPricingClient → POST {PricingBaseUrl}/prices"]
        LabCap["ILabCapabilityClient → GET {LabBaseUrl}/capabilities"]
        Coating["ICoatingClient → POST {CoatingBaseUrl}/coatings"]
        Shipment["IShipmentClient → POST {ShipmentBaseUrl}/shipments"]
    end

    API -.->|configured HttpClient base URLs, not invoked| Connectors

    subgraph EventOut ["Event egress"]
        Outbox["OutboxDispatcher"]
        Kafka["Kafka topic: rxflow.order.v1"]
    end
    Orders --> Outbox
    OrdersCancel --> Outbox
    Outbox --> Kafka
```

**Notes**

- Business endpoints today: `POST /orders`, `GET /orders/{id}`, `POST /orders/{id}/cancel` (all `[Authorize]`), plus `GET /health` (anonymous) and the reporting endpoints on `ReportsController`. `POST /orders/{id}/cancel` calls `CancelOrderService`, which transitions the order to `Rejected` via the existing domain rule, persists it through `IOrderRepository.UpdateAsync`, and writes an `OrderCancelled.v1` outbox record — it does not enqueue a Hangfire job. The `lab-override` authorization policy is defined in `Program.cs` but no endpoint currently requires it — there is no lab-override workflow implemented yet (open item in `Requirements.md`).
- The four outbound connectors (`Pricing`, `LabCapability`, `Coating`, `Shipment`) are wired into DI with configurable base URLs (`ConnectorOptions`) but are not called from `SubmitOrderService` or `OrderWorkflowJob` today — pricing actually goes through the in-process `LocalPriceCalculator`, not `PricingClient`.
- CORS is scoped to a single allowed origin (`http://localhost:5173`) for the local Vite frontend.
