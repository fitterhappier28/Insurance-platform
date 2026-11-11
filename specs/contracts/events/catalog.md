# Event catalog

Status: accepted, MVP scope. Decisions: [ADR-0005](../../../docs/adr/0005-transactional-outbox-and-inbox.md), [ADR-0006](../../../docs/adr/0006-event-carried-treaty-state.md), [ADR-0007](../../../docs/adr/0007-topic-per-aggregate-and-event-versioning.md).

## Topics

One topic per aggregate. The topic name carries a version that changes only if the key or partitioning changes; event types carry their own version.

| Topic | Key | Partitions | Retention | Producer | Event types | Consumers |
| --- | --- | --- | --- | --- | --- | --- |
| `clp.policy.v1` | `policyId` | 6 | 14 days | Policy | `policy-quoted.v1`, `policy-bound.v1` | Ledger (`policy-bound.v1` only) |
| `clp.carrier.v1` | `treatyId` | 1 | infinite | Carrier | `treaty-version-published.v1` | Ledger |
| `clp.ledger.v1` | `policyId` | 6 | 14 days | Ledger | `premium-booked.v1` | Policy |

Dead-letter topics: `<topic>.<consumer>.dlt`, for example `clp.policy.v1.ledger.dlt`. Retention infinite.

`clp.carrier.v1` keeps every Treaty Version forever so a new or rebuilt consumer can replay the full treaty history. Treaty volume is tiny.

`policy-quoted.v1` has no consumer in the MVP. It is published so that analytics and future services can follow the quote funnel without asking Policy.

## Envelope

CloudEvents 1.0, structured JSON mode in the Kafka message value. Schema: [envelope.schema.json](envelope.schema.json).

| Attribute | Example | Rule |
| --- | --- | --- |
| `specversion` | `1.0` | constant |
| `id` | `0193a1c2-...` | unique event id; consumers dedupe on it |
| `source` | `urn:clp:policy` | producing service |
| `type` | `clp.policy.policy-bound.v1` | `clp.<service>.<event>.v<major>` |
| `subject` | `policy/0193a1c2-...` | aggregate type and id |
| `time` | `2026-11-11T15:04:05.123Z` | when the state change committed |
| `datacontenttype` | `application/json` | constant |
| `dataschema` | `https://clp.example/schemas/events/policy-bound.v1.json` | schema id |
| `tenantid` | `harbor` | required extension |
| `correlationid` | uuid | required extension, from the originating request |
| `causationid` | uuid | optional extension, id of the event or request that caused this one |
| `traceparent` | W3C | optional extension |
| `data.aggregateSeq` | `2` | gap-free per aggregate, assigned in the same transaction as the state change |

Kafka headers duplicate `ce_id`, `ce_type` and `ce_tenantid` so consumers and tooling can filter without parsing the body.

Money in `data` is a string. Enumerations in `data` may grow; consumers must tolerate unknown values.

## Event types

| Type | Schema | Emitted when | Key facts |
| --- | --- | --- | --- |
| `clp.policy.policy-quoted.v1` | [policy-quoted.v1.schema.json](policy-quoted.v1.schema.json) | A Quote is created with any Decision | policy, revision, outcome, reasons, premium if priced |
| `clp.policy.policy-bound.v1` | [policy-bound.v1.schema.json](policy-bound.v1.schema.json) | A Quote is bound | policy number, term, Written Premium, versions, every Exposure with hierarchy ids and Rating Factors |
| `clp.carrier.treaty-version-published.v1` | [treaty-version-published.v1.schema.json](treaty-version-published.v1.schema.json) | A Treaty Version is published | treaty, version code, line, effective-from, Carrier shares |
| `clp.ledger.premium-booked.v1` | [premium-booked.v1.schema.json](premium-booked.v1.schema.json) | A Premium Transaction is appended | transaction, policy, revision, premium, treaty version, allocation totals per Carrier |

`policy-bound.v1` carries the full Rate Snapshot so Ledger can book without calling Policy or Rating. Event-carried state is deliberate here: the bound snapshot is immutable, and the Ledger must be reconstructable from what it received.

## Delivery and consumption rules

1. **Publish.** Producers insert into `OutboxMessage` in the same transaction as the state change. A relay publishes rows in `Sequence` order with `acks=all` and idempotent producer enabled, then sets `PublishedAt`. Rows may be published more than once; never zero times.
2. **Dedupe.** Consumers insert `(consumer, eventId)` into `InboxMessage` in the same transaction as their effect. A duplicate key means the event was already handled: commit the offset and move on.
3. **Business key.** Where a business identity exists, it is also unique: Ledger's `(tenantId, policyId, revisionNo, transactionType)`; Ledger's treaty view by `treatyVersionId`; Policy's booking by `(policyId, revisionNo)`. This protects against the same fact re-published under a new event id.
4. **Ordering.** Kafka guarantees order per key only. `aggregateSeq` lets a consumer drop a stale or duplicate event for an aggregate (`seq <= lastApplied`). Consumers that subscribe to only some types on a topic do not treat gaps as errors.
5. **Unknowns.** Unknown fields are ignored. Unknown event types on a subscribed topic are acknowledged and skipped. Unknown enum values a consumer must act on cause the message to be parked.
6. **Retries.** Transient failures retry in-process with exponential backoff (5 attempts, 1 s doubling, capped at 30 s). After that the message is written to the consumer's dead-letter topic with `ce_error` and `ce_attempts` headers, and the offset is committed.
7. **Re-drive.** `scripts/redrive.ps1 -Topic <dlt>` re-publishes parked messages to the source topic unchanged. Inbox and business keys make this safe for every consumer.
8. **Replay.** Rebuilding a read model means: stop the consumer, truncate its read model and its inbox rows, reset its consumer group to the earliest offset, start. Only read models are rebuilt this way; the premium ledger is never rebuilt from events. Runbook: [operations.md](../../../docs/architecture/operations.md).

## Versioning

Per [../README.md](../README.md#versioning-rules). Additive optional fields keep the type and bump the schema minor (`$comment` records it). Breaking changes create `.v2`; the producer publishes `.v1` and `.v2` side by side until every consumer has moved.
