# Acceptance evidence

## 2026-08-28

- `dotnet restore RxFlow.slnx` — passed under SDK 10.0.400.
- `dotnet build RxFlow.slnx --no-restore` — passed with 0 warnings and 0 errors.
- Domain tests — 3 passed.
- Application tests — 2 passed.
- Infrastructure persistence test — 1 passed (EF InMemory provider).
- `docker compose -f deploy/docker-compose.yml config` — passed.
- `docker compose -f deploy/docker-compose.yml up -d` — not executed: Docker Desktop Linux daemon was unavailable (`dockerDesktopLinuxEngine` named pipe missing).

Compose runtime, PostgreSQL migration execution, frontend build, and end-to-end scenario evidence remain unverified until Docker and Node/npm are available.

Additional quality-gate run:

- `dotnet restore RxFlow.slnx` — passed.
- `dotnet build RxFlow.slnx --no-restore` — passed, 0 warnings/errors.
- `dotnet test RxFlow.slnx --no-restore --no-build` — passed (6 tests total).
- `dotnet format RxFlow.slnx --verify-no-changes --no-restore` — passed.
- API build after adding authenticated `GET /orders/{id}` — passed, 0 warnings/errors.
- Fresh full verification after outbox/adapter changes: restore and build passed; all 6 tests passed.
- EF migration generation — passed for `InitialOrderSchema` and `AddOutboxMessages`; database apply/downgrade against PostgreSQL remains unverified.

## 2026-08-28 — Reporting boundary + frontend rebuild (Phase 7)

Scope: implemented `RxFlow.Reporting` (`OrderStatusView`, `IOrderReportReader`, `OrderReportingService`), `EfOrderReportReader` and an in-memory reader, `GET /reports/orders/{id}` and `GET /reports/orders`, and rebuilt `frontend/` from a single-file stub into a real order-form/status-view/lab-override client. Also fixed two pre-existing bugs found along the way: `EfOrderRepository` was never registered even with `Persistence:ApplyMigrations=true` (orders always used the in-memory store regardless of config), and the local `dotnet run` port (`5158`) didn't match the port Compose and the frontend actually expect (`5080`).

Verified:

- `dotnet build RxFlow.slnx` — passed, 0 warnings/errors, after adding `RxFlow.Reporting` and the new `EfOrderReportReader`/`ReportsController` code.
- `dotnet format RxFlow.slnx --verify-no-changes` — passed.
- `dotnet test RxFlow.slnx` — passed, 9 tests total (6 previous + 2 new `EfOrderReportReader` tests + kept the existing 1 rewritten).
- Manually booted `dotnet run --project src/RxFlow.Api`: `GET /health` returns `200 {"status":"ok"}`; `POST /orders`, `GET /reports/orders/{id}` correctly return `401` with no bearer token (auth middleware active as expected).

Not verified / explicitly blocking a real run of the app end-to-end:

1. **Frontend `npm install` has not completed.** `frontend/package.json`'s dependency pins included at least one nonexistent published version (`@vitejs/plugin-react@5.2.1`) and one incompatible pin (`typescript-eslint@8.19.1` doesn't support TypeScript 5.9.2, since corrected to `8.68.0`). Until install succeeds, `npm run build`/`lint`/`format`/`test` — all written, none executed — remain unverified. This is the immediate next step.
2. **No local auth token issuer/fixture exists.** `Auth:Authority` is unset in `appsettings.json` and D-007 in `DECISIONS.md` is still "proposed," not implemented. Every `[Authorize]` endpoint (`POST /orders`, both `/reports/orders` routes) will reject any request without a validly-signed JWT — so the order form, status view, and lab-override panel cannot complete a real request against a live API yet, only reach the 401 stage. This predates this session's work and is Phase 3 scope.
3. **Local dependency stack (Postgres/Redis/Redpanda) is not running.** Not required to run the app in default (in-memory, `ApplyMigrations=false`) config, but required for the persistence-backed path (`EfOrderRepository`/`EfOrderReportReader`) and for `docker compose up` to be exercised.
4. **Neither process has actually been started together for a live session** — `dotnet run --project src/RxFlow.Api` and (once install works) `npm run dev` in `frontend/`.

The lab-override workflow itself remains intentionally unimplemented on the backend (open item, `Requirements.md`); the new frontend panel calls the real (currently unmapped) route and reports "not implemented" rather than faking success — see `frontend/src/components/LabOverridePanel.tsx`.
