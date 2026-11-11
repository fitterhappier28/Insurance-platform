# 08: Ledger: treaty read model

**What to build:** Ledger keeps its own copy of every published Treaty Version by consuming `treaty-version-published`, idempotently. This ticket introduces the consumer host, inbox, retry and dead-letter handling in `Platform.Messaging`.

**Blocked by:** 06

**Status:** ready-for-agent

**Requirements:** FR-9.1, FR-9.2, FR-15.3 to FR-15.5

**Read first:** [ADR-0006](../../../docs/adr/0006-event-carried-treaty-state.md), [events catalog](../../contracts/events/catalog.md#delivery-and-consumption-rules), [operations.md](../../../docs/architecture/operations.md#consumer-groups)

- [ ] Ledger DB migrations for the treaty read model and `messaging` tables; runtime login per NFR-17.
- [ ] Consumer group `ledger.treaty-read-model` on `clp.carrier.v1` stores each Treaty Version with its shares and carrier names, under the event's tenant (FR-9.1).
- [ ] Same event delivered twice: one row, one inbox entry (FR-15.3).
- [ ] Same Treaty Version re-published with a new event id: still one row (business key `treatyVersionId`).
- [ ] Event with an extra unknown field is applied; an unknown event type on the topic is acknowledged and skipped (FR-15.4).
- [ ] A handler that keeps failing is retried per the catalog policy, then written to `clp.carrier.v1.ledger.dlt` with `ce_error`, `ce_attempts`, `ce_source_topic` headers, and the offset is committed (FR-15.5).
- [ ] Ledger has no HTTP client or configuration pointing at Carrier (FR-9.2; architecture test).
- [ ] Offsets are committed only after the database transaction commits.
