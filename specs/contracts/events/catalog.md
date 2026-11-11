# Event catalog

Status: proposed learning scope.

| Topic | Key | Producer | Event | Consumer |
| --- | --- | --- | --- | --- |
| `clp.policy.v1` | `policyId` | Policy | `policy-bound.v1` | Ledger |
| `clp.ledger.v1` | `policyId` | Ledger | `premium-booked.v1` | Policy |

Dead-letter topics use `<topic>.<consumer>.dlt`. Kafka delivery is at least once. Consumers use Inbox event-id deduplication and a business uniqueness constraint for financial booking.

## Envelope

Events use CloudEvents 1.0 structured JSON. Tenant, correlation, trace, event identity, subject, timestamp, schema URI and aggregate sequence are required as defined in [envelope.schema.json](envelope.schema.json). Money is a decimal string. Consumers ignore unknown optional fields and must safely handle duplicate deliveries.

## Events

| Type | Emitted when | Data |
| --- | --- | --- |
| `clp.policy.policy-bound.v1` | Policy Revision binds | Policy identifiers, term, written premium, ruleset/rate-manual versions, Exposure hierarchy identifiers, and complete Rate Snapshot |
| `clp.ledger.premium-booked.v1` | Ledger appends transaction | Transaction and policy identifiers, revision, written premium, booked timestamp |

`policy-bound` is an immutable fact containing the inputs Ledger requires. Ledger does not fetch current Policy or Rating state, so later edits or outages cannot change the premium that was bound.

## Delivery and transactions

1. Policy commits the bound revision and outbox row in one Policy database transaction.
2. The relay publishes pending rows and marks them published. A crash between publish and marking can cause duplicates.
3. Ledger commits the Inbox receipt, unique NewBusiness transaction and `premium-booked` outbox row in one Ledger transaction.
4. Policy commits the Inbox receipt and Booking Status projection update in one Policy transaction.
5. Retries are bounded; exhausted messages go to a consumer-specific dead-letter topic. A runbook documents inspection and safe redrive.

The initial scope uses one event version per topic and a stable partition key. More elaborate topic-per-aggregate schemes, infinite retention, and generic replay infrastructure are deferred until justified by a concrete consumer or recovery need.
