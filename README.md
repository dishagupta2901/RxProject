# RxFlow

RxFlow is a .NET 8 corporate training lab about prescription-to-lens order processing and Rx-lab routing. It is a synthetic teaching artefact, not a production product.

The repository is currently in the implementation-preparation phase. Project boundary directories now exist, but no application or infrastructure code has been created yet. Start with:

- [Agents.md](Agents.md) for working rules and the planned (not yet created) folder structure.
- [subagents.md](subagents.md) for later collaboration roles.
- [architecture.md](architecture.md) for the proposed system shape and data flow.
- [Requirements.md](Requirements.md) for known requirements, assumptions, and unresolved questions.
- [DECISIONS.md](DECISIONS.md) for decisions made before implementation.
- [docs/implementation-plan.md](docs/implementation-plan.md) for the phased coding sequence and exit checks.

The planned implementation separates a React/TypeScript client in `frontend/` from backend projects under `src/`; tests, deployment, and migration boundaries are under `tests/`, `deploy/`, and `database/`.

When implementation begins, all services and test dependencies must remain local and isolated through Docker Compose. Do not use real patient, prescription, corporate, production, or shared-infrastructure data.

## Documentation status

Commands and runtime behavior are intentionally not claimed until an implementation exists and the acceptance commands have been run. The future README will document restore, build, test, format, analysis, vulnerability, Compose, migration, and end-to-end order-flow commands with actual evidence.
