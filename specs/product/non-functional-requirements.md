# Non-functional requirements

Status: proposed learning requirements. Existing NFR identifiers are stable. Retired scope keeps its identifier unused.

These requirements describe verifiable engineering properties, not production commitments.

## Performance

| Id | Requirement | Evidence/status |
| --- | --- | --- |
| NFR-1 | Record quote latency for a representative, documented local workload. No pass/fail threshold is set until schedule size and concurrency are defined. | Measurement report |
| NFR-2 | Record Rating latency for the same documented workload. | Measurement report |
| NFR-3 | Record bind latency separately from asynchronous booking. | Measurement report |
| NFR-4 | Measure bind-to-Booked delay under normal and recovery conditions. | Measurement report; no invented threshold |

The vacancy's sub-second rating objective is a candidate production target, not an accepted local threshold.

## Availability and resilience

| Id | Requirement | Evidence/status |
| --- | --- | --- |
| NFR-5 | Quote and bind require Policy, Rules, Rating, and Policy storage. Kafka and Ledger outages after bind do not undo a successful bind. | Dependency failure tests |
| NFR-6 | Retired identifier; no active requirement. | Out of scope |
| NFR-7 | Outbox and consumers recover after Kafka restart without losing or duplicating business effects. | Failure/recovery acceptance scenario |
| NFR-8 | Policy-to-Rules/Rating calls have bounded timeouts and limited retries. Retries do not create side effects. | Client policy tests |
| NFR-9 | Services expose liveness/readiness health checks; readiness reflects dependencies required to serve that service's work. | Health checks |

## Integrity and audit

| Id | Requirement | Evidence/status |
| --- | --- | --- |
| NFR-10 | Money and rates use decimal types end to end with explicit scale and rounding; no binary floating point. | Domain and contract tests |
| NFR-11 | Ledger runtime credentials cannot UPDATE or DELETE ledger records; corrections, if later supported, are new transactions. Migrations use a separate owner credential. | Database permission integration test |
| NFR-12 | A Premium Transaction reconciles to its premium lines and Factor Lines to the cent. | Ledger self-check and tests |
| NFR-13 | Bound Policy Revisions are immutable. | Domain and persistence tests |
| NFR-14 | Stored business rows have UTC creation timestamps; policy effective dates are distinct from system timestamps. | Schema review and tests |
| NFR-15 | Events and telemetry carry tenant and correlation/trace context so booking can be followed from bind through Ledger. | End-to-end trace inspection |

## Isolation and security

| Id | Requirement | Evidence/status |
| --- | --- | --- |
| NFR-16 | Policy and Ledger own separate databases; each service is wired only to its own database. Separate local SQL Server instances are a demonstration choice, not the security guarantee. | AppHost ownership test |
| NFR-17 | Stateful contexts use distinct credentials scoped to their own databases. | Configuration/permission tests |
| NFR-18 | Tenant isolation is proven: each tenant-owned query is filtered and cross-tenant reads return not-found. | Service integration tests |
| NFR-19 | Local secrets are supplied through user secrets or environment-specific secret stores, never committed. | Repository/configuration review |
| NFR-20 | Applicant FEIN is excluded from logs, traces, and metrics. | Logging tests/review |

Local `X-Tenant-Id` is a test harness convention behind a trusted boundary, not authentication. Production must derive tenant identity from validated Entra ID/OIDC claims and enforce authorization at ingress and service boundaries.

## Evolvability and contracts

| Id | Requirement | Evidence/status |
| --- | --- | --- |
| NFR-21 | Architecture checks enforce BCL-only Domain projects, no service-to-service project references, and technical-only shared Platform code. | Architecture tests |
| NFR-22 | OpenAPI and event schemas are authoritative; active responses and produced events validate against them. | Contract tests |
| NFR-23 | Consumers tolerate compatible additive event fields and safely handle duplicate delivery. | Compatibility/consumer tests |
| NFR-24 | Each active service can build and package independently of local orchestration. | Per-service build/container build |

## Observability

| Id | Requirement | Evidence/status |
| --- | --- | --- |
| NFR-25 | Services emit structured logs to stdout without sensitive applicant data. | Local inspection |
| NFR-26 | OpenTelemetry traces cross HTTP and Kafka boundaries and export to the local dashboard. | Trace walkthrough |
| NFR-27 | Metrics expose request latency, outbox backlog, consumer lag, and dead-letter count. | Dashboard inspection |

## Operability

| Id | Requirement | Evidence/status |
| --- | --- | --- |
| NFR-28 | One documented command starts the local learning topology and its resources become healthy. | Clean-machine walkthrough |
| NFR-29 | Each stateful service owns and applies its own schema migrations. | Integration/startup tests |
| NFR-30 | A runbook explains outbox recovery, dead-letter handling, safe redrive, and booking reconciliation. | Runbook walkthrough |

## Future Azure objectives

The vacancy lists 99.9% availability, RPO <= 15 minutes, and RTO <= 4 hours. Keep them as candidate production objectives only. The repository has no workload profile, service hours, recovery-cost analysis, or operational ownership to justify acceptance thresholds. Before adopting them, define dependencies and workload, select backup/failover patterns, and exercise restore and failover.

The Azure deployment view is a design artifact, not a deployed MVP. Any implementation proposal should address private networking, managed identity, Key Vault, database isolation and recovery, event retention/replay, telemetry, CI/CD, infrastructure as code, and cost. Select Container Apps versus AKS and Event Hubs versus managed Kafka after operational and compatibility requirements are known.

## Architecture review notes

- The previous fixed latency and booking targets had no workload basis; they are now measurement objectives pending profile definition.
- Physical separation into one SQL Server container per context does not itself prove security. Database ownership, identity, permissions, and private network controls are the meaningful production properties.
- Application-level immutability does not stop privileged database edits. Restrict runtime permissions and preserve an audited correction path.
- Ledger is an immutable financial record, not a disposable read model. Do not describe truncation and replay as a routine way to rebuild it.
- Booking Status is eventually consistent. The API must distinguish successful binding from completed booking and expose Pending explicitly.
