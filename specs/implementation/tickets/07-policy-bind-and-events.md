# 07: Policy: bind and policy events

**What to build:** A broker binds a Straight-Through Quote and gets a policy number with Booking Status `Pending`; `policy-bound` (carrying the full Rate Snapshot) reaches Kafka through Policy's outbox. Quotes also publish `policy-quoted`.

**Blocked by:** 05, 06

**Status:** ready-for-agent

**Requirements:** FR-7, FR-15.1, FR-15.6, NFR-13

**Read first:** [ADR-0013](../../../docs/adr/0013-bind-re-rates-and-carries-the-snapshot.md), [policy-bound.v1.schema.json](../../contracts/events/policy-bound.v1.schema.json), [policy-quoted.v1.schema.json](../../contracts/events/policy-quoted.v1.schema.json)

- [ ] Every quote (any Decision) enqueues `policy-quoted.v1` in the quote transaction.
- [ ] Bind of the reference Quote returns `200`, `CP-2026-000001`, status `Bound`, Booking Status `Pending` (FR-7.4).
- [ ] Policy numbers are sequential per tenant and year; concurrent binds never share a number.
- [ ] `Referred` or `Declined` returns `409 quote-not-bindable` (FR-7.1); after 30 days `409 quote-expired` (FR-7.2, via `TimeProvider`).
- [ ] Bind re-runs Rules and Rating; a changed outcome, reason or premium returns `409 quote-stale` and changes nothing (FR-7.3).
- [ ] Bound revision, Rate Snapshot, idempotency record and outbox row commit in one transaction; a forced failure leaves none (FR-7.5).
- [ ] Any mutation of a bound revision fails in the domain model (FR-7.6, NFR-13).
- [ ] Replayed bind with the same key returns the original response; one outbox row exists (A6).
- [ ] `policy-bound.v1` on `clp.policy.v1` keyed by `policyId` validates against its schema and carries every Exposure with hierarchy ids and factors; `aggregateSeq` is 1 for quoted, 2 for bound.
- [ ] Rules or Rating down at bind returns `503` and the revision stays `Quoted`.
