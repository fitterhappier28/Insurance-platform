---
status: accepted
---

# Transactional outbox for publishing, inbox for consuming

A service never writes to its database and publishes to Kafka as two separate steps, because either can fail after the other succeeds. It inserts the event into its own `OutboxMessage` table in the same transaction as the state change, and a relay publishes it afterwards. Delivery is therefore at-least-once, so every consumer records the event id in `InboxMessage` in the same transaction as its effect, and a business unique key catches the same fact re-published under a new id.

## Considered Options

- Kafka transactions. Rejected: they cover Kafka-to-Kafka, not SQL Server to Kafka.
- Change data capture (Debezium) on the outbox table. Valid later; one more moving part for the MVP.
- Distributed transactions (MSDTC). Rejected: not available across separate instances in containers, and couples availability.

## Consequences

- Booking is visible a few seconds after bind, not in the bind response. Policy shows Booking Status `Pending` until `premium-booked` arrives.
- The relay runs as one instance per service in the MVP; multiple replicas would need a lease.
- Outbox rows are kept after publish. They are the archive used to re-publish if Kafka loses data.
