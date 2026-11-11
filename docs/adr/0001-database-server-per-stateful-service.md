---
status: accepted, private-network clause superseded by ADR-0014
---

# A separate SQL Server per stateful service

Policy, Carrier and Ledger each run their own SQL Server instance with their own logins (originally also on a private network shared only with their service; see ADR-0014). One instance hosting three databases would be lighter, but a shared engine makes "no cross-service database access" a convention rather than a fact: one wrong connection string or a cross-database query and the boundary is gone. Separate instances also let us show a service surviving the loss of another service's database.

## Considered Options

- One instance, database and login per service, plus a test that the logins cannot cross. Cheaper on memory; isolation depends on permissions being right.
- One instance, schema per service. Rejected: invites joins across ownership.

## Consequences

- About 2 GB of memory per instance; the stack needs roughly 10 GB for Docker.
- No distributed transactions are possible, by design. Cross-service consistency is eventual, through the outbox (ADR-0005).
- Each instance has its own backup and restore story in production.
