# Dynamic view: quote, bind and book

```mermaid
sequenceDiagram
  actor Broker
  participant Policy
  participant Rules
  participant Rating
  participant Kafka
  participant Ledger
  actor Finance
  Broker->>Policy: Create quote + schedule
  Policy->>Rules: Decide (pinned ruleset)
  Rules-->>Policy: Decision + reasons
  Policy->>Rating: Rate (pinned rate manual)
  Rating-->>Policy: Rate Snapshot + factors
  Policy-->>Broker: Quote
  Broker->>Policy: Bind + Idempotency-Key
  Policy->>Rules: Re-evaluate pinned version
  Policy->>Rating: Re-rate pinned version
  Policy->>Policy: Commit Bound revision + outbox
  Policy-->>Broker: Bound, Booking Pending
  Policy->>Kafka: policy-bound (outbox relay)
  Kafka->>Ledger: policy-bound (at least once)
  Ledger->>Ledger: Inbox + immutable Premium Transaction
  Ledger->>Kafka: premium-booked (outbox relay)
  Kafka->>Policy: premium-booked
  Policy->>Policy: Update Booking Status
  Finance->>Ledger: Read transaction and factor lines
```

## Transaction boundaries

- Quote: Policy stores the revision and snapshots after successful Rules and Rating calls. Rules and Rating remain stateless for request data.
- Bind: Policy commits bound state and `policy-bound` outbox row atomically. Kafka is not part of the request transaction.
- Booking: Ledger commits its Inbox receipt, immutable financial rows, and `premium-booked` outbox row atomically.
- Confirmation: Policy consumes `premium-booked` idempotently and updates Booking Status. A temporary delay leaves the status Pending; it does not undo the bound policy.

`policy-bound` carries all facts Ledger needs, including the complete Rate Snapshot and hierarchy identifiers. This makes booking independent of Policy and Rating availability after bind and preserves the exact bound price for audit.
