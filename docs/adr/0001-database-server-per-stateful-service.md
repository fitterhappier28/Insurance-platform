---
status: accepted, private-network clause superseded by ADR-0014
---

# A separate SQL Server per stateful service

Policy and Ledger each own a separate database and credentials. The local learning topology uses separate SQL Server instances to make ownership visible and exercise independent failure. This is a local demonstration choice, not a production security guarantee; production isolation depends on identity, permissions, network policy, and operational controls.

## Considered Options

- One SQL Server instance hosting a database and login per service. Cheaper on resources; separation depends on permissions and connection configuration.
- One instance, schema per service. Rejected: invites joins across ownership.

## Consequences

- Separate local instances consume more memory than a shared development SQL Server. The local topology is allowed to use a shared host if ownership remains testable.
- No distributed transactions are possible, by design. Cross-service consistency is eventual, through the outbox (ADR-0005).
- Each instance has its own backup and restore story in production.
