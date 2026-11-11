---
status: accepted
---

# Kafka (KRaft) as the event backbone, kept portable to Event Hubs

The platform uses Apache Kafka in KRaft mode through the `Confluent.Kafka` client. Only the Kafka protocol features that Azure Event Hubs also supports are used: keyed topics, consumer groups, idempotent producers. No Kafka transactions, Streams or compacted-topic semantics are relied on for correctness. Moving to Event Hubs or Confluent Cloud is a configuration change, not a code change.

## Consequences

- Retention limits differ by host; long-term history lives in each service's outbox archive and ledger, never only in the log.
