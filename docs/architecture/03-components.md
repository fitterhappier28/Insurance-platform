# Components

Structurizr views: `PolicyComponents`, `LedgerComponents`. Rules, Rating and Carrier are simple enough not to need a component view.

Every service uses the same layering: `Api` depends on `Application`, `Application` depends on `Domain` and on ports it defines, `Infrastructure` implements those ports. `Domain` depends on nothing outside the BCL (enforced by architecture tests, NFR-21).

## Policy Service

```mermaid
C4Component
  title Components: Policy Service

  Container_Ext(rules, "Rules Service")
  Container_Ext(rating, "Rating Service")
  ContainerDb_Ext(policyDb, "Policy DB")
  ContainerQueue_Ext(kafka, "Kafka")

  Container_Boundary(policy, "Policy Service") {
    Component(api, "Policy HTTP API", "Minimal APIs", "/v1 endpoints; tenant and idempotency middleware")
    Component(quote, "Quote use case", "Application", "Revision 1, Rules then Rating, store Decision and Rate Snapshot")
    Component(bind, "Bind use case", "Application", "Re-decide, re-rate, bind, enqueue policy-bound")
    Component(bookingConsumer, "Booking status consumer", "Kafka consumer", "Applies premium-booked")
    Component(reconciliation, "Reconciliation query", "Application", "Bound revisions still Pending")
    Component(domain, "Policy domain model", "Domain", "Hierarchy, invariants, immutability")
    Component(rulesClient, "Rules client", "HttpClient", "Timeout, retry, circuit breaker")
    Component(ratingClient, "Rating client", "HttpClient", "Timeout, retry, circuit breaker")
    Component(persistence, "Policy persistence", "EF Core", "Tenant filters, outbox, inbox, idempotency")
    Component(relay, "Outbox relay", "Platform.Messaging", "Publishes unsent rows in order")
  }

  Rel(api, quote, "Create quote")
  Rel(api, bind, "Bind")
  Rel(api, reconciliation, "Unbooked report")
  Rel(quote, rulesClient, "Decide")
  Rel(quote, ratingClient, "Rate")
  Rel(quote, domain, "Builds revision")
  Rel(quote, persistence, "Saves")
  Rel(bind, rulesClient, "Re-decide")
  Rel(bind, ratingClient, "Re-rate")
  Rel(bind, domain, "Binds")
  Rel(bind, persistence, "Saves with outbox row, one transaction")
  Rel(bookingConsumer, persistence, "Booking Status with inbox row")
  Rel(reconciliation, persistence, "Queries")
  Rel(rulesClient, rules, "POST /v1/decisions")
  Rel(ratingClient, rating, "POST /v1/rates")
  Rel(persistence, policyDb, "TDS")
  Rel(relay, policyDb, "Reads outbox")
  Rel(relay, kafka, "clp.policy.v1")
  Rel(bookingConsumer, kafka, "clp.ledger.v1")
```

Project mapping:

| Component | Project |
| --- | --- |
| Policy HTTP API | `Policy.Api` |
| Quote, Bind, Booking status, Reconciliation | `Policy.Application` |
| Policy domain model | `Policy.Domain` |
| Rules and Rating clients, persistence, consumer host | `Policy.Infrastructure` |
| Outbox relay, inbox, idempotency, tenancy | `Platform.*` |

## Ledger Service

```mermaid
C4Component
  title Components: Ledger Service

  ContainerDb_Ext(ledgerDb, "Ledger DB")
  ContainerQueue_Ext(kafka, "Kafka")

  Container_Boundary(ledger, "Ledger Service") {
    Component(api, "Ledger HTTP API", "Minimal APIs", "Transactions, self-check")
    Component(treatyConsumer, "Treaty consumer", "Kafka consumer", "Maintains treaty read model")
    Component(policyBoundConsumer, "Policy-bound consumer", "Kafka consumer", "Retries, then dead-letters")
    Component(booking, "Booking use case", "Application", "Effective Treaty Version, append transaction")
    Component(domain, "Ledger domain model", "Domain", "Transaction, lines, allocation, rounding")
    Component(persistence, "Ledger persistence", "EF Core", "Insert-only tables, read model, inbox, outbox")
    Component(relay, "Outbox relay", "Platform.Messaging", "Publishes premium-booked")
  }

  Rel(api, persistence, "Reads")
  Rel(treatyConsumer, kafka, "clp.carrier.v1")
  Rel(treatyConsumer, persistence, "Treaty version with inbox row")
  Rel(policyBoundConsumer, kafka, "clp.policy.v1")
  Rel(policyBoundConsumer, booking, "Book")
  Rel(booking, domain, "Builds transaction and allocation")
  Rel(booking, persistence, "Appends with inbox and outbox rows, one transaction")
  Rel(persistence, ledgerDb, "TDS")
  Rel(relay, ledgerDb, "Reads outbox")
  Rel(relay, kafka, "clp.ledger.v1")
```

### Booking algorithm

1. Inbox check for `(ledger.policy-bound, eventId)`. Seen: acknowledge.
2. Business key check for `(tenantId, policyId, revisionNo, NewBusiness)`. Exists: record inbox row, acknowledge.
3. Select the tenant's Treaty Version for `CP` with the latest `effectiveFrom` on or before the policy `effectiveFrom`. None: throw a retryable error (retries, then dead-letter).
4. For each Exposure: premium line, Factor Lines from the event, Allocation Lines per [the allocation rule](../../specs/product/rules-and-rating.md#quota-share-allocation).
5. Insert transaction, lines, inbox row, `AggregateSequence` increment and `premium-booked` outbox row in one SQL transaction.
