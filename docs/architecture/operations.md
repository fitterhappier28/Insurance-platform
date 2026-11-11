# Operations learning notes

These procedures describe the learning topology. They are not production runbooks.

## Outbox backlog

Inspect pending outbox rows and relay health for the owning service. Resolve connectivity or serialization errors before retrying. Outbox records remain durable until successfully published; duplicate publication is expected and consumers must be idempotent.

## Dead-letter handling

Inspect the consumer name, source event, attempt count, and failure reason. Fix the underlying cause, then re-drive the unchanged event through the source topic. Verify the business uniqueness key and Booking Status afterward. Never edit a bound Policy Revision or a Premium Transaction to repair a message.

## Booking reconciliation

Compare Policy revisions still Pending beyond the configured threshold with Ledger transactions keyed by tenant, policy, and revision. If Ledger has the transaction but Policy missed `premium-booked`, re-drive the event. If no transaction exists, inspect the original `policy-bound` delivery and consumer state.

## Local failure exercise

Stop Kafka or Ledger after bind. Confirm the bind response remains successful and Policy shows Pending. Restore the dependency and confirm one Premium Transaction and eventual Booked status. Repeat event delivery to demonstrate idempotency.

Replay and redrive tooling should be added only with the consumer and recovery scenario that needs it. The MVP does not rebuild the append-only Ledger by replaying all historical events.
