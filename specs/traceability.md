# Traceability

This map connects skills from the source vacancy to active learning scope. Status describes intended coverage, not implementation completion.

## Vacancy coverage

| Vacancy capability | Active evidence | Status |
| --- | --- | --- |
| Greenfield architecture and bounded contexts | [Architecture](../docs/architecture/README.md), [data ownership](product/data-model.md), ADRs | Active |
| Commercial Lines domain hierarchy | [Data model](product/data-model.md), Policy contracts | Active |
| Rating and underwriting separation | FR-3 to FR-6; Rules and Rating contracts | Active |
| API contract design and versioning | OpenAPI 3.1 and [contract rules](contracts/README.md) | Active |
| Event-driven architecture, Kafka, CQRS, eventual consistency | FR-11, FR-15; [event catalog](contracts/events/catalog.md); Policy/Ledger views | Active |
| Transactional outbox and idempotent consumers | FR-7, FR-10, FR-14, FR-15; ADR-0005 | Active |
| Auditable premium and decimal precision | FR-10, FR-12; data model and rating rules | Active |
| Azure design and production NFR mapping | [Future Azure view](../docs/architecture/05-deployment.md), NFR future targets | Design only |
| Reinsurance abstraction, quota share, bordereaux | FR-8/9 are retired and unused | Out of scope |
| SSO, AMS, enrichment, payments | Explicitly deferred | Deferred |

## Requirement map

| Requirement | Contract/design source | Planned evidence |
| --- | --- | --- |
| FR-1 to FR-3: account, schedule, NAICS code | Policy OpenAPI; data model | Policy API/domain tests |
| FR-4: Decision | Rules OpenAPI; rules-and-rating.md | `FR04_reference_exposure_is_straight_through`, `FR04_unsupported_naics_declines_with_reason`, `FR04_protection_class_ten_declines`, `FR04_all_reasons_are_returned_and_decline_dominates_refer`; matching Rules API tests; 11 passing |
| FR-5: Rate Snapshot | Rating OpenAPI; rules-and-rating.md | `FR05_reference_exposure_has_deterministic_decimal_factor_steps`, `FR05_old_building_uses_age_factor_and_rounds_to_cents`, `FR05_unrateable_exposure_is_rejected`, `FR05_rounding_is_away_from_zero_at_a_half_cent`; matching Rating API tests; 9 passing |
| FR-6 to FR-7: Quote and Bind | Policy OpenAPI; user-flow.md | Policy command and integration tests |
| FR-8 to FR-9 | Retired identifiers | Out of scope |
| FR-10: premium booking | Ledger OpenAPI; `policy-bound` event schema | Ledger consumer/invariant tests |
| FR-11 to FR-12: Booking Status and reconciliation | `premium-booked` event; Policy/Ledger read contracts | Consumer and reconciliation tests |
| FR-13 to FR-16: tenancy, idempotency, messaging, versioning | Common API rules; event catalog | Cross-cutting integration and architecture tests |
| FR-17: reads | Policy and Ledger OpenAPI | API integration tests |
| NFR-1 to NFR-24 | Non-functional requirements | Per-requirement checks as implementation is built |

Ticket 02 evidence is in progress. The Rules and Rating domain and HTTP API tests cover the reference fixture, declines, referrals, unsupported NAICS, rateability errors, and decimal rounding. NFR-2 still needs a recorded workload latency measurement. Acceptance verification for the full local stack also depends on a working container runtime.
