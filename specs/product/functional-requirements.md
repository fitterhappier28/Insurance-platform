# Functional requirements

Status: accepted, MVP scope. Ids are stable; never renumber. Tests are named after the criterion they prove, for example `FR07_3_...`.

Sources: `V-n` is a vacancy ownership item and `P-n` a platform requirement, both listed in [../traceability.md](../traceability.md). Scenario values come from [user-flow.md](user-flow.md); pricing and eligibility from [rules-and-rating.md](rules-and-rating.md).

| Id | Title | Owner |
| --- | --- | --- |
| FR-1 | Account and Applicant | Policy |
| FR-2 | Policy schedule | Policy |
| FR-3 | NAICS Extensions | Rules, Policy |
| FR-4 | Decision | Rules |
| FR-5 | Rating | Rating |
| FR-6 | Quote | Policy |
| FR-7 | Bind | Policy |
| FR-8 | Treaties | Carrier |
| FR-9 | Treaty read model | Ledger |
| FR-10 | Booking | Ledger |
| FR-11 | Booking Status | Policy |
| FR-12 | Reconciliation | Policy, Ledger |
| FR-13 | Tenancy | all |
| FR-14 | Idempotent commands | all |
| FR-15 | Event publication and consumption | Policy, Carrier, Ledger |
| FR-16 | Versioned contracts | all |
| FR-17 | Read endpoints | Policy, Carrier, Ledger |

## Policy service

### FR-1 Account and Applicant

Source: V-4.

1. A broker creates an Account with exactly one Applicant (legal name, FEIN, mailing address). Response `201` with `accountId`.
2. An Account is readable by id within the same tenant.
3. FEIN format is `NN-NNNNNNN`; anything else is `422 validation-failed`.

### FR-2 Policy schedule

Source: V-4.

1. A quote request carries the policy term and a schedule: one or more Locations, each with one or more Buildings, each with zero or more Units; one or more Coverages; one or more Exposures.
2. Every Exposure references one Coverage by code and exactly one Building or Unit on the same schedule. Dangling references are `422 validation-failed`.
3. Every Exposure carries a limit, a NAICS code and a NAICS Extension (`schema`, `data`).
4. The schedule is stored as Policy Revision 1 of a new Policy, with `effectiveFrom` and `effectiveTo` equal to the policy term. The term is 1 to 18 months; the effective date is no earlier than today.
5. The only Coverage code in MVP is `BLDG`. Deductibles are 1,000, 2,500, 5,000, 10,000 or 25,000.

### FR-3 NAICS Extensions

Source: V-4.

1. Each supported NAICS code has one or more versioned extension schemas in [../contracts/naics/](../contracts/naics/). MVP: `531110.v1`, `722511.v1`.
2. An Exposure's `extension.schema` must belong to its NAICS code.
3. Rules validates `extension.data` against the schema; failures return `422 validation-failed` with a JSON pointer per invalid field (alternate flow A5).
4. Policy stores the extension payload verbatim with its schema id.

### FR-6 Quote

Source: V-6.

1. On a quote request Policy calls Rules, then calls Rating only if the Decision is not Decline.
2. Policy stores on the Policy Revision: the Decision (outcome, reason codes, ruleset version) and, when priced, the Rate Snapshot (Written Premium, per-Exposure premium and Rating Factors, rate manual version).
3. Revision status is `Quoted` for Straight-Through, `Referred` for Refer, `Declined` for Decline.
4. The reference scenario returns `Quoted`, Written Premium `1410.75` and the four steps in [rules-and-rating.md](rules-and-rating.md#reference-calculation).
5. Alternate flows A1 to A4 return the statuses, reason codes and premiums listed in [user-flow.md](user-flow.md#alternate-flows).
6. A quote is valid for 30 days from creation.
7. If Rules or Rating is unavailable, the request fails with `503 dependency-unavailable` and nothing is stored.

### FR-7 Bind

Source: V-6, P-2, P-3.

1. Only a `Quoted` revision can be bound. `Referred` or `Declined` returns `409 quote-not-bindable`.
2. Binding after the quote validity window returns `409 quote-expired`.
3. Bind re-runs Rules and Rating with the stored schedule. If the outcome, any reason code or the Written Premium differs from the Quote, it returns `409 quote-stale` and nothing changes.
4. A successful bind assigns a policy number `CP-{yyyy}-{nnnnnn}` (per tenant sequence), sets the revision status to `Bound` and `boundAt`, and returns `200` with Booking Status `Pending`.
5. The bound revision, its Rate Snapshot, the idempotency record and the `policy-bound` outbox row are committed in one transaction.
6. A bound revision is immutable: any later write attempt fails in the domain model.

### FR-11 Booking Status

Source: V-2, V-5.

1. Policy consumes `premium-booked` and sets the revision's Booking Status to `Booked` with `transactionId` and `bookedAt`.
2. Booking Status is visible on the policy read endpoint.
3. Applying `premium-booked` twice has no further effect.

### FR-12 Reconciliation

Source: V-5.

1. Policy exposes a report of bound revisions whose Booking Status is still `Pending` after a configurable age (default 5 minutes).
2. Ledger exposes a self-check that, for every Premium Transaction, Allocation Lines sum to the premium line per Exposure and the last Factor Line equals the Exposure premium. Any mismatch is listed.

## Rules service

### FR-4 Decision

Source: V-6.

1. `POST /v1/decisions` returns outcome, ruleset version and all triggered reason codes per [rules-and-rating.md](rules-and-rating.md#ruleset-cp-rules-202607).
2. The quote-level outcome is the most severe Exposure outcome.
3. Rules stores nothing per request.
4. The ruleset version used is the latest published version effective on the request's effective date; a request may pin a version.

## Rating service

### FR-5 Rating

Source: V-6, V-5.

1. `POST /v1/rates` returns, per Exposure, the premium and every Rating Factor step (code, value, premium after step), plus total premium and rate manual version.
2. Results follow the formula and rounding in [rules-and-rating.md](rules-and-rating.md#precision-and-rounding).
3. Rating stores nothing per request and returns no eligibility information.
4. Inputs outside the manual return `422 not-rateable`.
5. The manual version used is the latest published version effective on the request's effective date; a request may pin a version.

## Carrier service

### FR-8 Treaties

Source: V-3.

1. Carriers Renaissance Re (`RENRE`) and Arch Re (`ARCHRE`) are seeded reference data.
2. A treaty administrator creates a Treaty for the tenant and line of business `CP`.
3. A treaty administrator publishes a Treaty Version with a version code, an effective-from date and Carrier shares. Shares are positive, have at most 4 decimal places, reference known Carriers, and sum to exactly 100.
4. A published Treaty Version is immutable. A change is a new Treaty Version with a later effective date.
5. Two Treaty Versions of the same Treaty cannot share an effective-from date.
6. Publishing commits the version and a `treaty-version-published` outbox row in one transaction.
7. Treaties and versions are readable within the tenant.

## Ledger service

### FR-9 Treaty read model

Source: V-3, V-2.

1. Ledger consumes `treaty-version-published` and stores the Treaty Version locally, keyed by tenant and treaty version id.
2. Ledger never calls Carrier. Booking works with Carrier stopped (alternate flow A9).
3. The read model can be dropped and rebuilt by replaying `clp.carrier.v1` from the beginning, producing the same rows.

### FR-10 Booking

Source: V-5, V-3.

1. On `policy-bound`, Ledger selects the tenant's Treaty Version for line `CP` with the latest effective-from on or before the policy effective date.
2. Ledger appends one Premium Transaction (type `NewBusiness`) with: one premium line per Exposure carrying policy, revision, Coverage, Location, Building, Unit and Exposure ids; the Factor Lines from the event's Rate Snapshot; and Allocation Lines per Exposure per Carrier using [the allocation rule](rules-and-rating.md#quota-share-allocation).
3. The transaction stores the treaty version id, rate manual version, ruleset version and source event id.
4. The reference scenario books 1,410.75 with Allocation Lines 987.53 (RENRE) and 423.22 (ARCHRE).
5. Ledger tables are insert-only.
6. At most one `NewBusiness` transaction exists per tenant, policy and revision, even if the event is delivered twice or re-published with a new id (A8).
7. Booking, the inbox row and the `premium-booked` outbox row commit in one transaction.
8. If no Treaty Version applies, booking is retried with backoff and then the event is parked on the dead-letter topic (A10).
9. A finance analyst lists Premium Transactions for a policy and reads one transaction with all its lines.

## Cross-cutting

### FR-13 Tenancy

Source: P-5.

1. Every HTTP request carries `X-Tenant-Id`. Missing is `400 tenant-required`; unknown is `403 tenant-unknown`. MVP tenants: `harbor`, `northstar`.
2. Every stored row carries the tenant id, and every query filters by it.
3. Reading another tenant's resource returns `404 not-found` (A11).
4. Every event carries the tenant id; consumers apply it to the same tenant.
5. Booking uses only the tenant's own Treaty Version; a Northstar policy is allocated 100% to Arch Re.

### FR-14 Idempotent commands

Source: P-2.

1. Every `POST` command requires `Idempotency-Key` (1 to 64 characters). Missing is `400 idempotency-key-required`. The side-effect-free calculations `POST /v1/decisions` and `POST /v1/rates` are exempt.
2. Keys are scoped by tenant and operation.
3. Repeating a key with the same body returns the stored status and body with `Idempotent-Replayed: true`, and performs no work (A6).
4. Repeating a key with a different body returns `422 idempotency-key-reused` (A7).
5. A concurrent request with an in-flight key returns `409 idempotency-key-in-flight`.
6. `5xx` outcomes are not stored, so the client can retry with the same key.
7. Keys are retained for at least 24 hours.

### FR-15 Event publication and consumption

Source: V-2, P-2, P-3.

1. Policy, Carrier and Ledger publish only through their outbox, per [../contracts/events/catalog.md](../contracts/events/catalog.md).
2. A relay publishes outbox rows in insertion order per aggregate and marks them published; a crash between publish and mark causes a re-publish, not a loss.
3. Every consumer records the event id in its inbox in the same transaction as its effect; a seen id is acknowledged without effect.
4. Consumers ignore unknown fields and tolerate unknown event types on a topic.
5. A message that still fails after the retry policy goes to `<topic>.<consumer>.dlt` with the failure reason; a script re-drives parked messages.
6. Every produced payload validates against its JSON Schema.

### FR-16 Versioned contracts

Source: V-1, P-4.

1. All routes are under `/v1`. The OpenAPI 3.1 file in [../contracts/openapi/](../contracts/openapi/) is written before the endpoint and served unchanged by the service at `/openapi/v1.yaml`.
2. Requests with unknown properties are rejected with `422 validation-failed`.
3. Errors are `application/problem+json` with a stable `code`.
4. Breaking changes follow [../contracts/README.md](../contracts/README.md#versioning-rules).

### FR-17 Read endpoints

Source: V-4, V-5.

1. Policy: read an Account, read a Quote (revision with Decision and Rate Snapshot), read a Policy (latest bound revision with Booking Status).
2. Carrier: list Carriers, list Treaties, read a Treaty with its versions.
3. Ledger: list Premium Transactions for a policy, read a Premium Transaction with all lines.

## Out of scope

Recorded so that nothing in the vacancy is dropped silently. Each has a row in [../traceability.md](../traceability.md).

- Underwriter workbench and referral resolution.
- Endorsements, cancellations, reinstatements, renewals; the model and ledger support them by adding revisions and transactions.
- Documents, billing, payments, bordereaux.
- SSO federation and per-user authorisation; MVP trusts `X-Tenant-Id` behind the gateway boundary (ADR-0008).
- AMS and clearinghouse connectivity (IVANS, EZLynx), third-party enrichment (property, loss history, credit/FCRA).
- Submission and clearance as a separate context.
- More lines of business, coverages and NAICS codes than listed.
- Azure deployment.
