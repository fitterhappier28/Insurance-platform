# Operations runbook

Local procedures for the stack run by the Aspire AppHost. Resources are started and stopped from the Aspire dashboard or the container runtime. Requirement: NFR-30. Rules: [events catalog](../../specs/contracts/events/catalog.md#delivery-and-consumption-rules).

## Consumer groups

| Consumer | Group id | Topic | Dead-letter topic |
| --- | --- | --- | --- |
| Ledger, treaty read model | `ledger.treaty-read-model` | `clp.carrier.v1` | `clp.carrier.v1.ledger.dlt` |
| Ledger, booking | `ledger.booking` | `clp.policy.v1` | `clp.policy.v1.ledger.dlt` |
| Policy, booking status | `policy.booking-status` | `clp.ledger.v1` | `clp.ledger.v1.policy.dlt` |

The inbox `Consumer` column uses the group id.

## Find unbooked policies

```powershell
Invoke-RestMethod http://localhost:5101/v1/reconciliation/unbooked?olderThanMinutes=5 -Headers @{ 'X-Tenant-Id' = 'harbor' }
```

A row here means `policy-bound` was published but `premium-booked` has not come back. Check, in order: Policy outbox backlog (`clp.outbox.pending`), the `ledger.booking` lag in Kafka UI, then `clp.policy.v1.ledger.dlt`.

## Check ledger integrity

```powershell
Invoke-RestMethod http://localhost:5105/v1/reconciliation/self-check -Headers @{ 'X-Tenant-Id' = 'harbor' }
```

Any item is a defect: the ledger is never fixed in place. Book a correcting transaction once that capability exists; until then, raise it.

## Re-drive dead-lettered messages

Typical cause: a policy was bound before its tenant's Treaty Version reached Ledger.

1. Fix the cause (publish the missing Treaty Version, confirm it appears in Ledger).
2. Re-publish parked messages to the source topic:

```powershell
./scripts/redrive.ps1 -DeadLetterTopic clp.policy.v1.ledger.dlt
```

The script copies each message's key, value and headers unchanged to the source topic named in its `ce_source_topic` header and advances a dedicated consumer group on the dead-letter topic, so a message is re-driven once. Every consumer of the source topic sees it again; inbox and business keys make that harmless.

## Rebuild Ledger's treaty read model

Use after a read-model bug fix or data loss. The premium ledger itself is never rebuilt.

```powershell
./scripts/rebuild-treaty-read-model.ps1
```

The script does the following:

1. Stops the `ledger-api` resource.
2. Deletes `TreatyShareView`, `TreatyVersionView` and the inbox rows for consumer `ledger.treaty-read-model`.
3. Resets group `ledger.treaty-read-model` on `clp.carrier.v1` to the earliest offset.
4. Starts the `ledger-api` resource and waits until the group lag is zero.

Bookings already made keep their stored `TreatyVersionId` and allocation; only future bookings read the rebuilt model.

## Re-publish from an outbox

If Kafka loses data (for example after removing the `kafka` data volume only):

```powershell
./scripts/republish-outbox.ps1 -Service carrier -From 2026-07-01
```

Clears `PublishedAt` for outbox rows created since the given date so the relay sends them again. Consumers deduplicate.

## Chaos checks used by acceptance tests

| Check | Command | Expected |
| --- | --- | --- |
| Carrier down (A9) | Stop resources `carrier-api` and `sql-carrier` | Quote, bind and booking succeed |
| Kafka restart (NFR-7) | Restart resource `kafka` during a bind burst | Every bind booked exactly once |
| Ledger down | Stop resource `ledger-api`, bind, start it | Booking Status reaches `Booked` after restart |
