---
status: proposed
---

# Redis read cache keyed by immutable version, instead of read replicas

Quick reads (quote and policy lookups, Treaty Version lookups, Ledger balances) are served from a per-service Redis cache-aside layer, not from SQL Server read replicas. Replicas triple the SQL Server footprint that ADR-0001 already makes heavy, and add replication lag and failover handling we do not need at MVP scale.

Cache keys carry the version of the thing cached: `{tenantId}:{service}:{type}:{id}:v{revision}`. Bound Policy Revisions and published Treaty Versions are immutable, so a versioned entry is never stale and needs no invalidation, only a TTL for memory hygiene. Mutable "current" views use a small pointer key (`...:{id}:current` holding the revision number) that is overwritten in the same request that commits the state change; a missed update costs one extra miss, never a wrong read of a bound revision.

## Considered Options

- SQL Server read replicas. Strong consistency options, but roughly 2 GB more memory per replica and lag handling. Rejected as too much for the need.
- In-process memory cache only. Cheapest, but not shared across instances and lost on restart.
- Redis with TTL-only invalidation. Rejected: stale quotes after a revision change.

## Consequences

- Redis is a cache, never a source of truth. Every read falls back to the owning service's own database; losing Redis degrades latency, not correctness.
- Each service uses its own Redis resource (or at least its own key prefix and credentials), so ADR-0001's "a service reads and writes only its own data" still holds. No service reads another's cache entries.
- Keys always include `tenantId` (ADR-0008). A test must show one tenant cannot hit another's key.
- Money in cached values stays string-encoded decimals, as in JSON.
- Writes never depend on Redis. State change and outbox insert stay in one SQL transaction (ADR-0005); the cache is updated after commit, best effort.
- Adds a Redis container to the AppHost (ADR-0014) and about 100 MB of memory per instance. Production target is Azure Cache for Redis.
- Follow-up before accepting: add an `NFR-n` read-latency target in `specs/product/`, and update `docs/architecture/workspace.dsl`.
