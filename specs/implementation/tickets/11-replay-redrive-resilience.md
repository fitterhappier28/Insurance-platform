# 11: Replay, re-drive and resilience

**What to build:** The operational procedures in the runbook work as written: treaty read model rebuild, dead-letter re-drive, outbox re-publish; and the platform survives Carrier, Ledger and Kafka outages without losing or duplicating money.

**Blocked by:** 10

**Status:** ready-for-agent

**Requirements:** FR-9.3, FR-15.5, NFR-5, NFR-6, NFR-7, NFR-23, NFR-30

**Read first:** [operations.md](../../../docs/architecture/operations.md), [04-dynamic-quote-bind-book.md](../../../docs/architecture/04-dynamic-quote-bind-book.md#failure-behaviour-along-the-path)

- [ ] `scripts/rebuild-treaty-read-model.ps1` rebuilds identical rows from `clp.carrier.v1` (FR-9.3).
- [ ] `scripts/redrive.ps1`: bind before the treaty exists, see the message parked, publish the treaty, re-drive, see one booking and Booking Status `Booked` (FR-15.5, A10).
- [ ] `scripts/republish-outbox.ps1` re-sends outbox rows; consumers create no duplicates.
- [ ] Acceptance (resources stopped and started through the test host): Carrier and its database stopped; quote, bind and booking succeed (NFR-6, A9).
- [ ] Acceptance: Ledger stopped during 20 binds, then started; all 20 booked exactly once (NFR-5).
- [ ] Acceptance: Kafka restarted during a burst of 50 binds; all 50 booked exactly once (NFR-7).
- [ ] Contract test: Ledger consumes a `policy-bound.v1` payload with additional unknown fields (simulating a newer producer minor version) and books normally (NFR-23).
- [ ] Runbook updated with anything learned (NFR-30).
