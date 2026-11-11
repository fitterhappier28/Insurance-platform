# 01: Skeleton and local stack

**What to build:** `dotnet run --project backend/src/AppHost` starts five healthy services, three SQL Servers, Kafka with its topics and the tooling resources, all visible in the Aspire dashboard. Each service serves its contract and rejects requests without a valid tenant. Architecture tests guard the layering and the database wiring from the first commit.

**Blocked by:** 00

**Status:** in-progress

**Requirements:** FR-13.1, FR-16.1, FR-16.3, NFR-9, NFR-16, NFR-17, NFR-19, NFR-21, NFR-22, NFR-24 to NFR-29

**Read first:** [06-crosscutting.md](../../../docs/architecture/06-crosscutting.md), [05-deployment.md](../../../docs/architecture/05-deployment.md), [ADR-0014](../../../docs/adr/0014-aspire-apphost-for-local-orchestration.md), [contracts README](../../contracts/README.md)

- [ ] Solution with central package management and the project layout from 06-crosscutting (AppHost, all services, `Platform.Tenancy`, `Platform.Web`, test projects); empty projects are fine where a later ticket fills them.
- [ ] `Platform.Web`: problem details with `code` and `traceId`, URL-segment API versioning, serving the embedded contract at `/openapi/v1.yaml` with Swagger UI at `/swagger`, `/health/live` and `/health/ready`, OpenTelemetry wiring (Aspire ServiceDefaults extension, plain OpenTelemetry), JSON console logs, correlation id middleware.
- [ ] `Platform.Tenancy`: `X-Tenant-Id` middleware; `400 tenant-required` and `403 tenant-unknown` (FR-13.1); tenants from configuration.
- [ ] Each of the five APIs starts, serves its contract file from `specs/contracts/openapi/` and reports healthy.
- [ ] AppHost resources as in 05-deployment: five projects on fixed ports 5101-5105; `sql-policy`, `sql-carrier`, `sql-ledger` each with its own database, data volume and 2 GB limit; `kafka` (KRaft) with the topics and dead-letter topics from the events catalog created at start; `kafka-ui`; `structurizr` bind-mounting `docs/architecture`. Start order through `WaitFor`.
- [ ] Each service references only its own database resource; Rules and Rating reference none (NFR-16).
- [ ] Per database an owner login and a runtime login scoped to it (NFR-17). Passwords are AppHost secret parameters generated into user secrets; nothing secret in the repository (NFR-19). Each service applies its migrations with the owner login on start, then serves with the runtime login (NFR-29).
- [ ] Container image per service builds with `dotnet publish -t:PublishContainer` (NFR-24).
- [ ] Architecture tests (NFR-21): Domain references BCL only; Application references only its Domain; no service references another service; Platform references no service; only AppHost references Aspire hosting packages. A test over the AppHost model asserts every database resource has exactly one referencing service (NFR-16).
- [ ] Acceptance tests start the AppHost with `Aspire.Hosting.Testing`: all five `/health/ready` return 200 (NFR-9); a request without `X-Tenant-Id` returns `400 tenant-required` on every service; one database's credentials are rejected by the other two SQL Server instances (NFR-16, NFR-17).
- [ ] Contract test helper validates an HTTP response against an operation in an OpenAPI file (NFR-22), used by the health and tenant tests.
- [ ] Structurizr Lite renders all six views from `workspace.dsl` without errors.
- [ ] README "Running locally" works as written.
