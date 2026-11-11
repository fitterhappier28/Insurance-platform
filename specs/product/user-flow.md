# User flow: Commercial Property quote, bind and premium booking

Status: proposed learning scope.

## Purpose

Demonstrate a contract-first Commercial Property flow with explicit domain ownership, separate eligibility and pricing, an immutable bound revision, reliable event publication, and an auditable premium ledger. This is an architecture exercise, not a production-ready insurance platform.

## Reference scenario

USD. Bayview Properties LLC has one commercial building in Tampa, Florida, built in 1998. The policy term is 2026-12-01 through 2027-12-01. Building coverage limit is 500,000, deductible 5,000, construction JoistedMasonry, protection class 5, NAICS 531110. Ruleset `CP-RULES-2026.07` returns Straight-Through. Rate Manual `CP-RATE-2026.07` produces Written Premium 1,410.75.

## Main flow

1. Broker creates an Account and its Applicant in Policy.
2. Broker submits a quote request containing the policy term and schedule. Policy validates ownership and references, creates a draft Policy Revision, then calls Rules and Rating using versioned contracts.
3. Rules validates the NAICS extension and returns a Decision. Rating independently returns a Rate Snapshot. Policy stores both snapshots with the Quote. A Decline has no price; a Refer has a price but cannot bind in this learning scope.
4. Broker binds an eligible Quote with an idempotency key. Policy re-evaluates eligibility and price using the versions pinned to the Quote. If unavailable or materially changed, bind fails without changing the Quote. On success, Policy makes the revision immutable and, in one database transaction, saves `policy-bound` to its outbox.
5. The Policy outbox relay publishes `policy-bound` to Kafka. Delivery is at least once.
6. Ledger consumes the event. In one local transaction it records the inbox receipt and appends an immutable Premium Transaction with premium lines and the Rating Factors carried in the bound snapshot. A unique business key prevents duplicate booking if the event is republished with a new event id.
7. Ledger saves `premium-booked` to its outbox in the same transaction. Its relay publishes the event. Policy consumes it idempotently and updates the separate Booking Status projection to Booked.
8. A finance analyst reads the Premium Transaction and verifies that the factor steps reconstruct the bound premium.

## Outcomes and failure cases

| Case | Outcome |
| --- | --- |
| Straight-Through and priced | Quote may be bound |
| Refer and priced | Quote is visible; bind is refused pending future underwriting workflow |
| Decline | Quote has reasons and no premium; bind is refused |
| Rate/rules version no longer available | Bind fails safely; no bound state or outbox event |
| Same bind key and same request | Original response is replayed |
| Same bind key and different request | `422 idempotency-key-reused` |
| Duplicate `policy-bound` delivery | One Premium Transaction |
| Kafka or Ledger unavailable after bind | Bind remains successful; Booking Status stays Pending until delivery recovers |

## Explicitly deferred

Endorsements, cancellations, renewals, underwriter workflow, billing, payments, SSO, AMS integrations, third-party enrichment, and production Azure deployment. Azure mapping and deployment decisions are documented as a future architecture path, not implemented infrastructure.
