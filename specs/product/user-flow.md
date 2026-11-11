# User flow: Commercial Property quote, bind and booking

Status: accepted, MVP scope.

## Actors

| Actor | Tenant | Uses |
| --- | --- | --- |
| Treaty administrator | `harbor`, `northstar` | Carrier API |
| Broker | `harbor` | Policy API |
| Finance analyst | `harbor` | Ledger API |
| Broker | `northstar` | Policy API (isolation check) |

Tenants are program organisations (MGAs). Each has its own Treaty and sees only its own Accounts, Policies and Premium Transactions.

## Reference scenario

All amounts are USD. These values are normative: tests assert them.

| Item | Value |
| --- | --- |
| Account / Applicant | Bayview Properties LLC, FEIN 12-3456789 |
| Policy term | 2026-12-01 to 2027-12-01 |
| Location | 1200 Harbor Blvd, Tampa, FL 33602, protection class 5 |
| Building | Joisted masonry, built 1998, 24,000 sq ft |
| Units | 101 retail (12,000 sq ft), 102 office (12,000 sq ft) |
| Coverage | Building, deductible 5,000 |
| Exposure | Building coverage on the Building, limit 500,000, NAICS 531110 |
| NAICS extension `531110.v1` | unitCount 2, percentVacant 0, hasRestaurantTenant false |
| Harbor Treaty Version | `HBR-CP-QS-2026.07`, effective 2026-07-01, Renaissance Re 70%, Arch Re 30% |
| Northstar Treaty Version | `NST-CP-QS-2026.07`, effective 2026-07-01, Arch Re 100% |
| Ruleset | `CP-RULES-2026.07` |
| Rate Manual | `CP-RATE-2026.07` |
| Written Premium | 1,410.75 |
| Allocation | Renaissance Re 987.53, Arch Re 423.22 |

Premium derivation and rounding are specified in [rules-and-rating.md](rules-and-rating.md).

## Main flow

1. **Publish treaties.** The treaty administrator for `harbor` creates the Harbor Commercial Property quota-share Treaty and publishes Treaty Version `HBR-CP-QS-2026.07`. Northstar does the same with `NST-CP-QS-2026.07`. Carrier emits `treaty-version-published`; Ledger stores each version in its local treaty read model.
2. **Create the account.** The Harbor broker creates Account "Bayview Properties LLC" with its Applicant.
3. **Request a quote.** The broker submits the policy term and the schedule: one Location, one Building with two Units, the Building Coverage, and one Exposure with its NAICS Extension.
4. **Decide and price.** Policy sends the Exposures to Rules. Rules validates the NAICS Extension against `531110.v1` and returns Straight-Through. Policy sends the Exposures to Rating, which returns 1,410.75 with four Rating Factors. Policy stores the Decision and the Rate Snapshot on Policy Revision 1 and returns the Quote with status `Quoted`.
5. **Bind.** The broker binds the Quote with an `Idempotency-Key`. Policy re-runs Rules and Rating, confirms the same Decision and premium, assigns policy number `CP-2026-000001`, marks Revision 1 bound, and in the same transaction writes `policy-bound` to its outbox. The response is `Bound` with Booking Status `Pending`.
6. **Book.** Ledger consumes `policy-bound`, selects the Harbor Treaty Version effective on 2026-12-01, and appends one Premium Transaction with one premium line, four Factor Lines and two Allocation Lines. It writes `premium-booked` to its outbox.
7. **Confirm.** Policy consumes `premium-booked` and sets Booking Status to `Booked`.
8. **Inspect.** The finance analyst reads the Premium Transaction for the policy and can rebuild 1,410.75 from the Factor Lines and see it split 987.53 / 423.22.

## Alternate flows

| Id | Trigger | Outcome |
| --- | --- | --- |
| A1 Refer for vacancy | `percentVacant` 40 | Decision Refer (`R-VACANCY`), premium returned, status `Referred`, bind refused with `409 quote-not-bindable` |
| A2 Refer for age | Building built 1972 | Decision Refer (`R-BUILDING-AGE`), premium 1,567.50, bind refused |
| A3 Decline for hood suppression | NAICS 722511, `hoodSuppression` false | Decision Decline (`D-HOOD-SUPPRESSION`), no premium, status `Declined` |
| A4 Decline for unsupported class | NAICS 447110 | Decision Decline (`D-NAICS-UNSUPPORTED`), no premium |
| A5 Invalid extension | `531110.v1` payload missing `unitCount` | `422 validation-failed` naming the field; nothing stored |
| A6 Repeated bind | Same `Idempotency-Key`, same body | Original response replayed with `Idempotent-Replayed: true`; one bound revision, one Premium Transaction |
| A7 Key reuse | Same `Idempotency-Key`, different body | `422 idempotency-key-reused` |
| A8 Duplicate event | `policy-bound` delivered twice, or re-published with a new event id | One Premium Transaction |
| A9 Carrier unavailable | Carrier service and its database stopped | Quote, bind and booking succeed using Ledger's treaty read model |
| A10 Treaty missing | No Treaty Version effective on the policy date | Booking retried, then parked on the dead-letter topic; Booking Status stays `Pending` and appears on the reconciliation report |
| A11 Cross-tenant read | Northstar broker reads Harbor's policy or transaction | `404 not-found` |
| A12 Stale quote | Rules or Rating result at bind differs from the Quote | `409 quote-stale`; broker re-quotes |
| A13 Expired quote | Bind more than 30 days after quote | `409 quote-expired` |

## Not in this flow

Underwriter work on referred quotes, endorsements, cancellations, documents, billing, bordereaux, broker sign-in (SSO), AMS downloads (IVANS, EZLynx), third-party enrichment and payments. See [functional-requirements.md](functional-requirements.md#out-of-scope).
