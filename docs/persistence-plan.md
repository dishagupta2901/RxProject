# Persistence implementation notes

The Infrastructure project owns EF Core/Npgsql mappings and implements `IOrderRepository` from the Application project. PostgreSQL remains the source of truth. The eventual migration set must contain four to six purposeful migrations and support clean apply plus downgrade against an ephemeral database.

EF Core package references, `DbContext`, mappings, and the initial order/outbox migrations are now present and build under the repository's .NET 10 override. Testcontainers-based PostgreSQL apply/downgrade checks remain pending. The in-memory provider is test-only and is not the production persistence decision.
