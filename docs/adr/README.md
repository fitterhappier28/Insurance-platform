# Architecture decision records

Format: [.agents/skills/domain-modeling/ADR-FORMAT.md](../../.agents/skills/domain-modeling/ADR-FORMAT.md). Record a decision only when it is hard to reverse, surprising without context, and the result of a real trade-off. Never edit an accepted ADR's decision; supersede it with a new one.

| ADR | Decision | Area |
| --- | --- | --- |
| [0001](0001-database-server-per-stateful-service.md) | A separate SQL Server per stateful service | Data ownership |
| [0002](0002-stateless-rules-and-rating.md) | Rules and Rating are stateless and load versioned files | Decomposition |
| [0003](0003-quote-is-a-policy-revision.md) | A Quote is a Policy Revision | Domain model |
| [0004](0004-account-inside-policy-service.md) | Account lives in the Policy service for the MVP | Decomposition |
| [0005](0005-transactional-outbox-and-inbox.md) | Transactional outbox and idempotent inbox | Messaging |
| [0006](0006-event-carried-treaty-state.md) | Ledger books from its own copy of Treaty Versions | Messaging, Carrier |
| [0007](0007-topic-per-aggregate-and-event-versioning.md) | Topic per aggregate, versioned event types | Messaging |
| [0008](0008-tenant-from-header-row-level-isolation.md) | Tenant from header, row-level isolation | Tenancy |
| [0009](0009-kafka-as-event-backbone.md) | Kafka, portable to Event Hubs | Infrastructure |
| [0010](0010-contract-first-url-major-versioning.md) | Contract-first, major version in URL | APIs |
| [0011](0011-shared-platform-libraries.md) | Shared technical libraries, no shared domain | Code structure |
| [0012](0012-ledger-allocates-and-stores-premium.md) | Ledger computes and stores allocation; insert-only | Ledger |
| [0013](0013-bind-re-rates-and-carries-the-snapshot.md) | Bind re-rates; policy-bound carries the Rate Snapshot | Policy, Ledger |
| [0014](0014-aspire-apphost-for-local-orchestration.md) | .NET Aspire AppHost orchestrates the local stack | Infrastructure |
| [0015](0015-redis-read-cache-keyed-by-immutable-version.md) | Redis read cache keyed by immutable version, no read replicas (proposed) | Infrastructure, Reads |
