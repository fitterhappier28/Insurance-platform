# Non-functional requirements

Status: accepted, MVP scope. Ids are stable; tests and checks cite them.

The MVP runs locally, orchestrated by a .NET Aspire AppHost (ADR-0014). Each requirement states the MVP target and, where they differ, the production target from the vacancy so the gap is explicit.

## Performance

| Id | Requirement | MVP target | Verified by |
| --- | --- | --- | --- |
| NFR-1 | Quote latency (Policy, Rules, Rating, single Exposure, warm) | p95 under 500 ms over 100 sequential requests | acceptance test |
| NFR-2 | Rating latency | p95 under 100 ms | Rating integration test |
| NFR-3 | Bind latency (excludes booking) | p95 under 500 ms | acceptance test |
| NFR-4 | Bind to Booking Status `Booked` | p95 under 5 s | acceptance test |

Production target: sub-second rating on the interactive quote path (vacancy).

## Availability and resilience

| Id | Requirement | MVP target |
| --- | --- | --- |
| NFR-5 | Quote and bind depend only on Policy, Rules, Rating and Policy's database | Carrier or Ledger down does not fail quote or bind |
| NFR-6 | Booking does not depend on Carrier at runtime | Carrier and its database stopped: booking succeeds (A9) |
| NFR-7 | Outbox relays and consumers survive Kafka restarts | No lost or duplicated business effect after the `kafka` resource is restarted |
| NFR-8 | Synchronous calls have timeouts and retries | Policy to Rules and Rating: 2 s timeout, 2 retries with jitter on transient errors, circuit breaker |
| NFR-9 | Health endpoints | `/health/live` and `/health/ready` (ready checks own database and Kafka where used) |

Production targets, not met by the MVP: 99.9% uptime on the quote and bind path, RPO 15 minutes or less, RTO 4 hours or less (vacancy). They require managed SQL with point-in-time restore, multi-zone hosting and managed Kafka or Event Hubs.

## Data integrity and audit

| Id | Requirement |
| --- | --- |
| NFR-10 | Money uses `decimal` in code, `decimal(19,4)` in SQL, strings in JSON. No binary floating point anywhere money or rates are handled. |
| NFR-11 | Ledger tables are insert-only: Ledger's runtime login is denied `UPDATE` and `DELETE` on the `ledger` schema. Migrations run under a separate owner login. |
| NFR-12 | Every Premium Transaction can be reconstructed to the cent from its Factor Lines and split by Carrier from its Allocation Lines (FR-12). |
| NFR-13 | Bound Policy Revisions and published Treaty Versions are immutable in the domain model and never updated in SQL. |
| NFR-14 | Every stored business row records `createdAt` (UTC) and, where applicable, its effective dates. |
| NFR-15 | Every event and log line carries `correlationId` and `tenantId`; a booking can be traced from the bind request through Kafka to the ledger rows. |

## Isolation and security

| Id | Requirement |
| --- | --- |
| NFR-16 | Physical database isolation: Policy, Carrier and Ledger each run their own SQL Server container. A service is wired only to its own database; the AppHost wiring and per-database credentials are tested (ADR-0014). |
| NFR-17 | Each service has its own SQL logins (owner for migrations, runtime for requests), scoped to its own database. No shared credentials. |
| NFR-18 | Tenant isolation is proven by acceptance tests (FR-13). |
| NFR-19 | Secrets are Aspire parameters, generated into user secrets on first run and never committed. The AppHost documents each parameter. |
| NFR-20 | Applicant data (FEIN) is not written to logs. |

Production targets, deferred: SSO federation with tenant from token claims, TLS between services, encryption at rest (TDE), FCRA controls for credit data.

## Evolvability

| Id | Requirement |
| --- | --- |
| NFR-21 | Architecture tests fail the build if a `*.Domain` project references infrastructure packages, if `Platform.*` contains domain types, or if one service references another service's projects. |
| NFR-22 | OpenAPI files and event schemas are the contract; contract tests validate live responses and produced events against them. |
| NFR-23 | A consumer deployed before a producer's additive change keeps working (unknown-field tolerance test). |
| NFR-24 | Each service builds, tests and ships as its own container image (`dotnet publish -t:PublishContainer`), independent of the AppHost. |

## Observability

| Id | Requirement |
| --- | --- |
| NFR-25 | Structured JSON logs to stdout. |
| NFR-26 | OpenTelemetry traces and metrics exported over OTLP to the Aspire dashboard; trace context propagates through HTTP and Kafka headers. |
| NFR-27 | Metrics: outbox backlog per service, consumer lag, dead-letter count, request latency per route. |

## Operability

| Id | Requirement |
| --- | --- |
| NFR-28 | `dotnet run --project src/AppHost` starts the full stack, with every resource healthy in the Aspire dashboard, on a machine with 10 GB available to Docker. |
| NFR-29 | Each service applies its own migrations on start. |
| NFR-30 | Runbook for replay and dead-letter re-drive lives in [../../docs/architecture/operations.md](../../docs/architecture/operations.md). |
