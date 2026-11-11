# 10: Policy: booking status and reconciliation

**What to build:** Policy shows Booking Status `Booked` once the Ledger confirms, lists bound policies that are still unbooked, and Ledger reports any transaction whose lines do not reconcile.

**Blocked by:** 09

**Status:** ready-for-agent

**Requirements:** FR-11, FR-12

**Read first:** [operations.md](../../../docs/architecture/operations.md), [policy.v1.yaml](../../contracts/openapi/policy.v1.yaml), [ledger.v1.yaml](../../contracts/openapi/ledger.v1.yaml)

- [ ] Consumer group `policy.booking-status` on `clp.ledger.v1` sets Booking Status `Booked`, `transactionId`, `bookedAt` (FR-11.1).
- [ ] `GET /v1/policies/{id}` shows the Booking Status (FR-11.2).
- [ ] Duplicate `premium-booked` has no further effect (FR-11.3).
- [ ] `GET /v1/reconciliation/unbooked` lists bound revisions still `Pending` past the age threshold and omits booked ones (FR-12.1).
- [ ] `GET /v1/reconciliation/self-check` on Ledger returns no items for correct data and lists a deliberately corrupted fixture (inserted with the owner login in the test) (FR-12.2).
- [ ] End-to-end in Testcontainers: bind, then Booking Status becomes `Booked` within 5 s.
