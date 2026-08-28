# Persistence implementation notes

The Infrastructure project owns EF Core/Npgsql mappings and implements `IOrderRepository` from the Application project. PostgreSQL remains the source of truth. The eventual migration set must contain four to six purposeful migrations and support clean apply plus downgrade against an ephemeral database.

EF Core package references, `DbContext`, mappings, migrations, and Testcontainers checks are intentionally deferred until the pinned .NET 8 SDK and package restore are available. No in-memory substitute is used as a production decision.
