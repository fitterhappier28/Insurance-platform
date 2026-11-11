# Functional requirements

Status: proposed learning scope. IDs remain stable; retired scope is marked deferred rather than renumbered.

The product flow and reference scenario are defined in [user-flow.md](user-flow.md). Requirements focus on architecture skills from the target role: commercial data, Rules/Rating separation, contract-first services, event-driven integration, and financial traceability.

| Id | Requirement | Owner | Scope |
| --- | --- | --- | --- |
| FR-1 | Account and Applicant | Policy | Active |
| FR-2 | Policy schedule and revisions | Policy | Active |
| FR-3 | Exposure classification | Policy | Active |
| FR-4 | Eligibility Decision | Rules | Active |
| FR-5 | Rate Snapshot | Rating | Active |
| FR-6 | Quote | Policy | Active |
| FR-7 | Bind | Policy | Active |
| FR-8 | Retired identifier | — | Not in scope |
| FR-9 | Retired identifier | — | Not in scope |
| FR-10 | Premium booking | Ledger | Active |
| FR-11 | Booking Status | Policy | Active |
| FR-12 | Reconciliation | Policy, Ledger | Active |
| FR-13 | Tenant isolation | All | Active |
| FR-14 | Idempotent commands | Stateful services | Active |
| FR-15 | Event publication and consumption | Policy, Ledger | Active |
| FR-16 | Versioned API and event contracts | All | Active |
| FR-17 | Read endpoints | Policy, Ledger | Active |

## FR-1 Account and Applicant — Policy

An Account has one Applicant in this learning scope. Policy owns both, validates FEIN format, and scopes reads by tenant.

## FR-2 Policy schedule and revisions — Policy

A quote contains a term, Coverages, Locations, Buildings, optional Units, and Exposures. Each Exposure references exactly one Coverage and exactly one Building or Unit on the same revision. A Policy Revision is immutable after bind. MVP creates one revision; endorsements and later revisions are deferred.

## FR-3 Exposure classification — Policy

An Exposure carries a six-digit NAICS code. Policy validates its format; Rules and Rating use the code as an input to their respective versioned eligibility and pricing configuration.

## FR-4 Eligibility Decision — Rules

Rules evaluates every Exposure and returns the most severe outcome plus all reason codes. Outcomes are Straight-Through, Refer, and Decline. Rules owns immutable versioned rulesets and stores no per-quote business state. It never calculates premium.

## FR-5 Rate Snapshot — Rating

Rating calculates per-Exposure premium and factor steps using an immutable versioned Rate Manual. It stores no per-quote business state and returns no eligibility result. All money uses decimal arithmetic and documented rounding.

## FR-6 Quote — Policy

Policy validates the schedule, calls Rules, and calls Rating unless the Decision is Decline. It stores the Decision and, when priced, the complete Rate Snapshot with the Policy Revision. A Quote pins the Ruleset and Rate Manual versions used. Quotes expire after 30 days. Refer is priced but not bindable in the MVP; Decline has no price.

## FR-7 Bind — Policy

Only an unexpired Straight-Through Quote can bind. Policy re-evaluates the stored schedule with the Quote's pinned ruleset and rate manual. A changed Decision, reason set, premium, or unavailable pinned version returns `409 quote-stale` and changes nothing. A successful bind assigns a policy number, marks the revision Bound, creates Booking Status `Pending`, and atomically persists those changes with the idempotency response and `policy-bound` outbox message. The bound revision and its snapshots are immutable; Booking Status is a separate projection.

## FR-10 Premium booking — Ledger

Ledger consumes `policy-bound` and appends one immutable NewBusiness Premium Transaction, with one Premium Line per Exposure and Factor Lines copied from the event's Rate Snapshot. The event is the authoritative input; Ledger does not call Policy or Rating. A unique `(tenantId, policyId, revisionNo, transactionType)` business key prevents duplicate effects. Booking, Inbox receipt, and `premium-booked` outbox row commit in one transaction.

## FR-11 Booking Status — Policy

Policy consumes `premium-booked` idempotently and updates a booking-status projection from Pending to Booked. Binding does not wait for Ledger. Policy exposes Pending while the asynchronous booking is outstanding.

## FR-12 Reconciliation — Policy, Ledger

Policy reports bound revisions that remain Pending beyond a configurable age. Ledger verifies that each transaction total equals its premium lines and that each final Factor Line equals its Exposure premium. Reconciliation is read-only and reports mismatches; corrections are new immutable records in any future scope.

## FR-13 Tenant isolation — All

Every request and event carries tenant identity. Every tenant-owned row and query is scoped to it. Cross-tenant reads return not-found. Tenant identity is trusted from a gateway header only in local learning scope; production authentication is deferred.

## FR-14 Idempotent commands — Stateful services

State-changing POST commands require `Idempotency-Key`. Reusing a key with the same canonical request replays the original response; reusing it with a different request returns `422`. Side-effect-free Rules and Rating calculations do not require keys.

## FR-15 Event publication and consumption — Policy, Ledger

Policy and Ledger publish state-change events only through transactional outboxes. Consumers persist Inbox receipts atomically with their effect and also enforce business uniqueness where applicable. Delivery is at least once; effects are idempotent. Failed events are retried and eventually dead-lettered with a documented replay procedure.

## FR-16 Versioned contracts — All

Public and inter-service HTTP APIs use OpenAPI 3.1 and `/v1` routes. Events have versioned JSON Schemas. Additive compatible changes preserve consumer compatibility; breaking changes use a new major contract version.

## FR-17 Read endpoints — Policy, Ledger

Policy reads Accounts, Quotes, bound Policies, and Booking Status. Ledger reads Premium Transactions and their lines. Each service reads only its own database.

## Deferred product scope

FR-8 and FR-9 are retired identifiers kept unused to preserve traceability. Also out of scope: underwriting workbench, endorsements, cancellation, renewal, billing, payments, bordereaux, SSO, AMS connectivity, third-party enrichment, and production Azure deployment. Azure target architecture is documented separately in the deployment view.
