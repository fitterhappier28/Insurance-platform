# Cross-cutting architecture

## Data ownership and layering

Policy and Ledger own separate databases. Rules and Rating are stateless application services with immutable versioned configuration. Domain projects depend only on the BCL; application code owns use cases; infrastructure implements persistence and messaging adapters; APIs translate contracts into application requests.

## CQRS

Policy commands (`CreateQuote`, `BindQuote`) enforce invariants and change Policy state. Policy queries read Accounts, Quotes, and Policies. Ledger's booking consumer appends records; its query API reads those records. The first implementation may use one database and a small read model per owner. CQRS here means distinct command and query responsibilities, not event sourcing or a mandatory second database.

## Tenancy

Requests and events carry tenant identity. Policy and Ledger repositories scope tenant-owned records and queries. A validated identity claim should supply the tenant in production; the local learning environment may use `X-Tenant-Id` behind a trusted gateway. Tenant identity is propagated through the event envelope and never inferred from a body field.

## Idempotency

State-changing HTTP commands require an idempotency key scoped by tenant and operation. A same-key/same-request retry returns the saved response; a different request is rejected. Kafka consumers use an Inbox receipt and a business uniqueness constraint in the same transaction as the effect.

## Transactional outbox and inbox

The service that owns a state change writes its event to a local outbox in the same database transaction. A relay publishes it to Kafka and may publish more than once. The consumer stores its Inbox receipt and business effect atomically, then commits the Kafka offset. No HTTP request handler publishes directly to Kafka.

## Contracts and compatibility

OpenAPI 3.1 defines HTTP contracts; JSON Schema defines events. Breaking changes require a major contract version. Event consumers tolerate duplicate delivery and additive optional fields. Stable error codes support client behavior.

## Resilience and observability

Rules and Rating calls use bounded timeouts and limited retries. OpenTelemetry traces cross HTTP and Kafka boundaries. Structured logs include tenant and correlation identifiers while excluding FEIN. Useful operational measures include request latency, outbox backlog, consumer lag, and dead-letter count.

## Local and future deployment

Aspire may orchestrate the local learning topology. It is not a production dependency. The future Azure mapping is in [05-deployment.md](05-deployment.md); managed hosting, identity, networking, recovery objectives and cost remain design topics rather than claims of production readiness.
