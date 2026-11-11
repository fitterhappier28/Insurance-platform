# 09: Ledger: book premium

**What to build:** When a policy is bound, Ledger appends one immutable Premium Transaction with premium, factor and allocation lines under the tenant's effective Treaty Version, publishes `premium-booked`, and lets a finance analyst read it.

**Blocked by:** 07, 08

**Status:** ready-for-agent

**Requirements:** FR-10, FR-13.5, FR-17.3, NFR-10, NFR-11, NFR-12, NFR-15

**Read first:** [ADR-0012](../../../docs/adr/0012-ledger-allocates-and-stores-premium.md), [03-components.md](../../../docs/architecture/03-components.md#booking-algorithm), [allocation rule](../../product/rules-and-rating.md#quota-share-allocation), [ledger.v1.yaml](../../contracts/openapi/ledger.v1.yaml)

- [ ] Ledger migrations for transaction and line tables; runtime login denied `UPDATE` and `DELETE` on the `ledger` schema, proven by a test that tries (NFR-11).
- [ ] Consumer group `ledger.booking` books the reference policy: written premium `1410.75`, four Factor Lines, Allocation Lines RENRE `987.53` (lead) and ARCHRE `423.22` (FR-10.2, FR-10.4).
- [ ] Treaty Version selected by policy effective date, not booking time; a later-effective version is ignored for this policy (FR-10.1).
- [ ] Transaction stores treaty version id, manual and ruleset versions, source event id (FR-10.3).
- [ ] Northstar policy is allocated 100% to ARCHRE (FR-13.5).
- [ ] Duplicate delivery and re-publish with a new event id both leave exactly one transaction (FR-10.6, A8).
- [ ] Transaction, inbox row and `premium-booked` outbox row commit together (FR-10.7); event validates against `premium-booked.v1`.
- [ ] No effective Treaty Version: retried, then dead-lettered to `clp.policy.v1.ledger.dlt` (FR-10.8, A10).
- [ ] Property test: for random premiums and share sets, Allocation Lines sum exactly to the premium; last Factor Line equals the premium (NFR-12).
- [ ] `GET /v1/policies/{id}/transactions` and `GET /v1/transactions/{id}` per contract, `404` across tenants (FR-10.9, FR-17.3).
- [ ] The bind request's `correlationId` appears on the ledger transaction log entries and on `premium-booked` (NFR-15).
