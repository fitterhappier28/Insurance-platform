---
status: accepted
---

# Tenant from a header, isolated by row inside each service database

Each request carries `X-Tenant-Id`, every row and event carries `tenantId`, and every repository applies a tenant filter (an EF Core global query filter plus an explicit tenant on every insert). A database per tenant would multiply the three SQL Servers by the number of tenants; row-level isolation keeps one schema per service and is proven by cross-tenant acceptance tests. The header stands in for the tenant claim of an SSO token, which replaces it when federation lands; until then the APIs must sit behind a gateway that sets the header.

## Considered Options

- Database per tenant per service. Strongest isolation, heavy to operate at this stage.
- SQL Server row-level security policies. A good second line of defence; deferred.

## Consequences

- A missing filter is a data leak, so each service has a test that every entity type is tenant-filtered.
- Idempotency keys, policy number sequences and treaties are tenant-scoped.
