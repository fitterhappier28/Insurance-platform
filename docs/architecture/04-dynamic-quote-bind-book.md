# Dynamic view: quote, bind, book

Structurizr view: `QuoteBindBook`. Scenario values: [../../specs/product/user-flow.md](../../specs/product/user-flow.md#reference-scenario).

```mermaid
C4Dynamic
  title Treaty publication, quote, bind and booking

  Person(treatyAdmin, "Treaty administrator")
  Person(broker, "Broker")
  Person(financeAnalyst, "Finance analyst")
  Container(carrier, "Carrier Service")
  ContainerDb(carrierDb, "Carrier DB")
  Container(policy, "Policy Service")
  ContainerDb(policyDb, "Policy DB")
  Container(rules, "Rules Service")
  Container(rating, "Rating Service")
  ContainerQueue(kafka, "Kafka")
  Container(ledger, "Ledger Service")
  ContainerDb(ledgerDb, "Ledger DB")

  Rel(treatyAdmin, carrier, "Publish HBR-CP-QS-2026.07")
  Rel(carrier, carrierDb, "Version and outbox row")
  Rel(carrier, kafka, "treaty-version-published")
  Rel(ledger, kafka, "Consume treaty version")
  Rel(ledger, ledgerDb, "Treaty read model and inbox")
  Rel(broker, policy, "POST /v1/quotes")
  Rel(policy, rules, "Decide: StraightThrough")
  Rel(policy, rating, "Rate: 1410.75")
  Rel(policy, policyDb, "Revision, Decision, Rate Snapshot")
  Rel(broker, policy, "POST bind")
  Rel(policy, rules, "Re-decide")
  Rel(policy, rating, "Re-rate")
  Rel(policy, policyDb, "Bound revision and outbox, one transaction")
  Rel(policy, kafka, "policy-bound")
  Rel(ledger, kafka, "Consume policy-bound")
  Rel(ledger, ledgerDb, "Transaction, lines, inbox, outbox")
  Rel(ledger, kafka, "premium-booked")
  Rel(policy, kafka, "Consume premium-booked")
  Rel(policy, policyDb, "Booking Status Booked")
  Rel(financeAnalyst, ledger, "GET transactions")
```

The numbered arrows are one walk of the reference scenario, not a second architecture. Treaty publication happens first so Ledger already holds the Treaty Version when `policy-bound` arrives. Quote and bind are synchronous HTTP through Policy, Rules and Rating. Booking is asynchronous: Policy commits the bind, the outbox relay publishes, Ledger appends the Premium Transaction, then Policy marks Booking Status Booked.

The sequence diagram below is the same walk with return values filled in (Decision, premium `1410.75`, allocation). It is the readable form of the C4 dynamic view.

## Sequence

```mermaid
sequenceDiagram
  autonumber
  actor Admin as TreatyAdmin
  actor Broker
  participant Carrier
  participant Policy
  participant Rules
  participant Rating
  participant Kafka
  participant Ledger
  actor Finance as FinanceAnalyst

  Admin->>Carrier: POST /v1/treaties/{id}/versions
  Carrier-->>Admin: 201 TreatyVersion
  Carrier->>Kafka: treaty-version-published (relay)
  Kafka->>Ledger: treaty version into read model

  Broker->>Policy: POST /v1/quotes
  Policy->>Rules: POST /v1/decisions
  Rules-->>Policy: StraightThrough, CP-RULES-2026.07
  Policy->>Rating: POST /v1/rates
  Rating-->>Policy: 1410.75 with 4 steps, CP-RATE-2026.07
  Policy-->>Broker: 201 Quote, status Quoted

  Broker->>Policy: POST /v1/quotes/{id}/bind + Idempotency-Key
  Policy->>Rules: re-decide
  Policy->>Rating: re-rate
  Policy-->>Broker: 200 Bound CP-2026-000001, booking Pending
  Policy->>Kafka: policy-bound (relay)

  Kafka->>Ledger: policy-bound
  Ledger->>Ledger: transaction 1410.75, RENRE 987.53, ARCHRE 423.22
  Ledger->>Kafka: premium-booked (relay)
  Kafka->>Policy: premium-booked
  Policy->>Policy: Booking Status Booked

  Finance->>Ledger: GET /v1/policies/{id}/transactions
  Ledger-->>Finance: transaction with factor and allocation lines
```

## Failure behaviour along the path

| Failure | Effect | Recovery |
| --- | --- | --- |
| Rules or Rating down during quote or bind | `503 dependency-unavailable`; nothing stored | Client retries with the same idempotency key |
| Policy crashes after commit, before relay publish | Outbox row unsent | Relay publishes on restart |
| Relay crashes after publish, before marking | Event published twice | Ledger inbox drops the duplicate |
| Kafka down | Bind still succeeds; booking waits | Relay resumes; Booking Status catches up |
| Ledger down | Booking waits | Consumer resumes from its committed offset |
| Treaty Version not yet in Ledger | Retries, then dead-letter | Re-drive after the treaty arrives ([operations.md](operations.md)) |
| Carrier down | No effect on quote, bind or booking | n/a |
