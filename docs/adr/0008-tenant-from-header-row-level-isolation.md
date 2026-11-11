---
status: accepted
---

# Tenant from a header, isolated by row inside each service database

Each request carries a tenant identity, every tenant-owned row and event carries that identity, and every repository applies a tenant filter. A database per tenant would multiply infrastructure with tenant count; row-level isolation keeps tenant data within each service-owned database and is proven by cross-tenant tests. The local `X-Tenant-Id` header stands in for a validated tenant claim; production must derive it from authenticated identity and enforce access through the gateway and services.

## Considered Options

- Database per tenant per service. Strongest isolation, heavy to operate at this stage.
- SQL Server row-level security policies. A good second line of defence; deferred.

## Consequences

- A missing filter is a data leak, so each service has a test that every entity type is tenant-filtered.
- Idempotency keys, policy number sequences and treaties are tenant-scoped.
