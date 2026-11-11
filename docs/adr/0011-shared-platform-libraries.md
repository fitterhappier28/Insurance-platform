---
status: accepted
---

# Shared technical libraries in src/Platform, no shared domain

Tenancy, idempotency, outbox relay, inbox, CloudEvents envelope, Kafka wrappers and web conventions are the same in every service and easy to get subtly wrong, so they live once in `src/Platform/` as small projects (`Platform.Tenancy`, `Platform.Idempotency`, `Platform.Messaging`, `Platform.Web`). Domain types never go there: a shared `Policy` or `Money` model would couple service releases and blur ownership. Services reference Platform as project references within this repository.

## Considered Options

- Versioned NuGet packages from a feed. Closer to separate-repo reality; adds release ceremony for one team.
- Copy the plumbing into each service. Maximum independence; five diverging outbox implementations.

## Consequences

- An architecture test fails the build if a Platform project references a service or defines domain types.
- A breaking change in Platform touches every service at once; keep Platform APIs small.
