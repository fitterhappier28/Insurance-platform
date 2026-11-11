# Cross-cutting concepts

How the platform handles concerns that every service shares. Implementations live in `src/Platform/` ([ADR-0011](../adr/0011-shared-platform-libraries.md)).

## Code layout

```
src/
  AppHost/                   .NET Aspire AppHost: resources, wiring, ports, parameters (ADR-0014)
  Platform/
    Platform.Tenancy/        tenant context, header middleware, EF Core tenant filter helpers
    Platform.Idempotency/    Idempotency-Key middleware and store (messaging.IdempotencyRecord)
    Platform.Messaging/      CloudEvents envelope, outbox writer and relay, inbox, Kafka producer and consumer host, dead-letter
    Platform.Web/            problem details, API versioning, contract serving, health, OpenTelemetry wiring (the Aspire ServiceDefaults extension lives here)
  Services/
    Policy/   Policy.Domain  Policy.Application  Policy.Infrastructure  Policy.Api
    Rules/    Rules.Domain   Rules.Application                         Rules.Api
    Rating/   Rating.Domain  Rating.Application                        Rating.Api
    Carrier/  Carrier.Domain Carrier.Application Carrier.Infrastructure Carrier.Api
    Ledger/   Ledger.Domain  Ledger.Application  Ledger.Infrastructure  Ledger.Api
tests/
  Architecture.Tests/        dependency rules across all projects (NFR-21)
  Policy.Tests/ Rules.Tests/ Rating.Tests/ Carrier.Tests/ Ledger.Tests/
  Platform.Tests/
  Acceptance.Tests/          end-to-end against the AppHost (Aspire.Hosting.Testing)
```

Rules and Rating have no `Infrastructure` project: they load their version files from the `Api` project's content folder through a port defined in `Application`.

Dependency rules:

| Project | May reference |
| --- | --- |
| `*.Domain` | BCL only |
| `*.Application` | its `Domain` |
| `*.Infrastructure` | its `Application`, its `Domain`, `Platform.*`, EF Core, Confluent.Kafka |
| `*.Api` | its `Application`, its `Infrastructure`, `Platform.*` |
| `Platform.*` | BCL, ASP.NET Core, EF Core, Confluent.Kafka; never a service project |
| A service project | never another service's projects |
| `AppHost` | service `Api` projects (for wiring) and Aspire hosting packages; nothing else references it |

## Tenancy

[ADR-0008](../adr/0008-tenant-from-header-row-level-isolation.md), FR-13.

1. Middleware reads `X-Tenant-Id`, validates it against configured tenants, and sets a scoped `ITenantContext`.
2. Every tenant-owned entity implements `ITenantOwned`. The DbContext applies a global query filter on `TenantId` and stamps it on insert; inserting with a mismatched tenant throws.
3. Outbox rows copy the tenant into the CloudEvents `tenantid` attribute. Consumers set `ITenantContext` from the event before invoking the handler, so the same filters apply.
4. Idempotency keys, policy number sequences and treaties are keyed by tenant.
5. A test per service enumerates the EF model and fails if any entity except reference data (Carrier) lacks the tenant filter.

## Idempotency

FR-14.

| Situation | Behaviour |
| --- | --- |
| New key | Insert `IdempotencyRecord` as `InFlight` with the request hash, run the handler, store the status and body as `Completed` in the handler's transaction |
| Same key, same hash, completed | Return stored status and body, header `Idempotent-Replayed: true` |
| Same key, different hash | `422 idempotency-key-reused` |
| Same key, in flight | `409 idempotency-key-in-flight` |
| Handler returns 5xx or throws | Transaction rolls back, record removed; retry is safe |

Request hash: SHA-256 of method, route template, route values and the canonical JSON body. Records expire after 24 hours.

Consumers are idempotent through the inbox and business keys ([events catalog](../../specs/contracts/events/catalog.md#delivery-and-consumption-rules)).

## Transactional outbox and inbox

[ADR-0005](../adr/0005-transactional-outbox-and-inbox.md), FR-15.

- `IOutbox.Enqueue(event)` adds an `OutboxMessage` to the current DbContext; it is saved by the same `SaveChanges` as the aggregate.
- `OutboxRelay` is a hosted service: every 250 ms it reads up to 100 unpublished rows ordered by `Sequence`, produces each with `acks=all`, idempotent producer and the aggregate id as key, and marks the batch published. It stops at the first failure so order is kept.
- `KafkaConsumerHost<THandler>` consumes with manual offset commit. For each message: parse the envelope, set tenant and trace context, open a transaction, insert the inbox row (duplicate means skip), run the handler, commit, then commit the offset. Retry and dead-letter policy as in the catalog.
- `aggregateSeq` comes from `AggregateSequence`, incremented in the same transaction.

## API versioning and contracts

[ADR-0010](../adr/0010-contract-first-url-major-versioning.md), FR-16.

- `Asp.Versioning.Http` with URL segment versioning; every endpoint group is mapped under `/v{version:apiVersion}` with version 1.0.
- The OpenAPI file is embedded and served at `/openapi/v1.yaml`; Swagger UI at `/swagger` renders it.
- Request DTOs are deserialised with `UnmappedMemberHandling.Disallow`, so unknown properties fail with `422 validation-failed`.
- Money is `decimal` in DTOs, serialised as a string by a shared converter.

## Error handling

All errors are problem details with a stable `code` ([contracts README](../../specs/contracts/README.md#error-model)). Domain errors are typed results mapped to codes in one place per service; unexpected exceptions become `500` with only the trace id exposed.

## Resilience

- Policy's Rules and Rating clients: `Microsoft.Extensions.Http.Resilience` standard pipeline, 2 s attempt timeout, 2 retries with jitter on transient failures, circuit breaker (NFR-8).
- Kafka producer: idempotent, `acks=all`, retries handled by the client.
- Consumers: in-process retry with exponential backoff, then dead-letter.

## Observability

NFR-15, NFR-25 to NFR-27.

- OpenTelemetry for ASP.NET Core, HttpClient, EF Core and a custom `ActivitySource` around produce and consume. `traceparent` travels in the CloudEvents envelope, so one trace spans bind, relay, Ledger booking and Policy's booking status update.
- Logs are structured JSON with `tenantId`, `correlationId`, `traceId`. FEIN is never logged (NFR-20).
- Metrics: `clp.outbox.pending`, `clp.consumer.lag`, `clp.dlt.messages`, HTTP server duration per route.
- Exported over OTLP to the Aspire dashboard, which the AppHost provides locally. Service code uses plain OpenTelemetry and `OTEL_EXPORTER_OTLP_ENDPOINT`; it does not reference Aspire hosting packages.

## Time and money

- Time comes from `TimeProvider`, injected, so tests control "now" (quote expiry, effective dates).
- Money and rates are `decimal` end to end. Rounding is `MidpointRounding.AwayFromZero` for premiums and `ToZero` for non-lead allocation shares, each in one function in the owning domain.
