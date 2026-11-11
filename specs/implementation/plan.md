# Implementation plan

Status: proposed learning roadmap for the simplified quote-to-bind architecture.

## Target sequence

| Step | Outcome | Skills demonstrated |
| --- | --- | --- |
| 1. Foundations | Local topology, service templates, health, telemetry, architecture rules | .NET architecture, local developer experience |
| 2. Rules and Rating | Versioned deterministic eligibility and pricing contracts | Domain separation, decimal correctness, versioning |
| 3. Policy quote | Tenant-scoped account and schedule, orchestration, snapshots | Commercial domain model, API design, CQRS commands/queries |
| 4. Policy bind | Idempotent bind, immutable revision, outbox transaction | Aggregate invariants, reliable state change |
| 5. Ledger booking | Kafka consumer, inbox/business dedupe, append-only premium and factor lines | Event-driven design, financial audit |
| 6. Status and reconciliation | Booking Status projection, pending report, self-check | Eventual consistency, CQRS read models, operations |
| 7. Hardening and deployment design | Failure exercise, contract compatibility, Azure architecture package | Resilience, observability, cloud architecture |

## Completion standard

For each active requirement, define the contract before implementation, add public-behavior tests, implement, and update [traceability.md](../traceability.md). Do not claim production SLOs or Azure deployment until validated and implemented. Keep service data ownership explicit and avoid adding a service without a distinct business responsibility or operational reason.

## Deferred scope

Underwriter workflow, endorsements, cancellation, renewals, billing, payments, SSO federation, AMS connectivity, and third-party enrichment.

## Tickets

The active tickets in [tickets/](tickets/) map one-to-one to the target sequence above. Start with the lowest-numbered ticket whose prerequisites are complete.
