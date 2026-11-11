---
status: accepted
---

# Ledger books from its own copy of Treaty Versions

Carrier owns Treaties and publishes every Treaty Version as `treaty-version-published` on a topic with infinite retention. Ledger keeps a local read model of those versions and allocates premium from it, rather than calling Carrier synchronously at booking time. Booking therefore keeps working when Carrier or its database is down, and the treaty used for every booked dollar is a row Ledger owns.

## Considered Options

- Synchronous resolve call from Ledger or Policy to Carrier at booking or bind. Simpler and always current, but makes Carrier a runtime dependency of the money path.

## Consequences

- A policy can be bound before its tenant's Treaty Version reaches Ledger. Ledger retries, then parks the event on the dead-letter topic; Booking Status stays `Pending` and shows on the reconciliation report until it is re-driven.
- The read model can be rebuilt by replaying `clp.carrier.v1` from the beginning. On a host with bounded retention (Event Hubs), the same rebuild re-publishes Carrier's outbox archive instead.
- Treaty Versions are immutable and effective-dated, so a copy can never go stale in meaning; it can only be late.
