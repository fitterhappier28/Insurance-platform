# Traceability

Every requirement traces from its source, through the specs, decisions and contracts, to the tickets that build it and the tests that prove it. Update this file in the same change as the code ([AGENTS.md](../AGENTS.md#spec-driven-workflow)).

Status values: `planned`, `in-progress`, `verified` (tests pass), `out-of-scope`.

## Sources

### Vacancy ownership items (V)

| Id | Item |
| --- | --- |
| V-1 | API contracts: OpenAPI 3.1 for inter-service and external APIs before engineering builds; versioning and backward compatibility |
| V-2 | Event-driven backbone: Kafka domain event model, schemas, versioning, replay semantics; eventual consistency |
| V-3 | Carrier and quota-share framework: Carrier Service abstraction, Renaissance Re and Arch Re at launch |
| V-4 | Commercial data model: Account, Applicant, Policy, Coverage, Location, Building, Unit/Exposure, NAICS-specific extensions |
| V-5 | Premium traceability: immutable Premium Transaction Ledger, factor-level reconstruction; effective-dating, reconciliation, NUMERIC precision |
| V-6 | Rating and rules separation: stateless Rating (price only), Rules (eligibility, referral, straight-through) |
| V-7 | Service decomposition: bounded contexts and data ownership per service |
| V-8 | Integration architecture: SSO, AMS/clearinghouse (IVANS, EZLynx), enrichment (property, loss, credit/FCRA), payments |
| V-9 | NFRs and governance: sub-second rating, 99.9% uptime, RPO 15 min, RTO 4 hr; guardrails (immutable transactions, effective-dating, no cross-service DB access) in every design and review |
| V-10 | Reinsurance: quota share, bordereaux, multi-carrier allocation |
| V-11 | Binding architecture artifacts: C4 diagrams, ADRs, API contracts, data models |

### Platform requirements (P)

| Id | Item |
| --- | --- |
| P-1 | Microservices with clean DDD layering |
| P-2 | Idempotency of commands and event handling |
| P-3 | Transactional outbox |
| P-4 | API versioning |
| P-5 | Multi-tenancy |
| P-6 | Stack: .NET, SQL Server, Docker, Kafka |
| P-7 | Spec-driven development, traced decisions, C4 documentation |

## Source coverage

| Source | Requirements | Decisions | Specs and contracts | Tickets | Status |
| --- | --- | --- | --- | --- | --- |
| V-1 | FR-16, NFR-22 | ADR-0010 | [contracts/README.md](contracts/README.md), [openapi/](contracts/openapi/) | 01, 02, all service tickets | planned |
| V-2 | FR-9, FR-11, FR-15, NFR-7, NFR-23 | ADR-0005, 0006, 0007, 0009 | [events/catalog.md](contracts/events/catalog.md), event schemas | 06, 07, 08, 09, 10, 11 | planned |
| V-3 | FR-8, FR-9, FR-10 | ADR-0006, 0012 | [carrier.v1.yaml](contracts/openapi/carrier.v1.yaml), [data-model.md](product/data-model.md#carrier-service) | 06, 08, 09 | planned |
| V-4 | FR-1, FR-2, FR-3, FR-17 | ADR-0003, 0004 | [data-model.md](product/data-model.md), [naics/](contracts/naics/), [policy.v1.yaml](contracts/openapi/policy.v1.yaml) | 03, 04, 05 | planned |
| V-5 | FR-10, FR-11, FR-12, NFR-10 to NFR-14 | ADR-0012, 0013 | [rules-and-rating.md](product/rules-and-rating.md#precision-and-rounding), [ledger.v1.yaml](contracts/openapi/ledger.v1.yaml) | 09, 10 | planned |
| V-6 | FR-4, FR-5, FR-6, FR-7, NFR-1, NFR-2 | ADR-0002, 0013 | [rules-and-rating.md](product/rules-and-rating.md), [rules.v1.yaml](contracts/openapi/rules.v1.yaml), [rating.v1.yaml](contracts/openapi/rating.v1.yaml) | 02, 03, 05, 07 | planned |
| V-7 | NFR-16, NFR-17, NFR-21, NFR-24 | ADR-0001, 0002, 0004, 0011, 0014 | [02-containers.md](../docs/architecture/02-containers.md) | 01 and every service ticket | planned |
| V-8 | none in MVP | ADR-0008 (tenant header stands in for SSO) | [01-system-context.md](../docs/architecture/01-system-context.md#scope) | none | out-of-scope |
| V-9 | NFR-1 to NFR-9, NFR-11, NFR-13, NFR-16, NFR-21 | ADR-0001, 0005, 0012 | [non-functional-requirements.md](product/non-functional-requirements.md), [AGENTS.md](../AGENTS.md#guardrails-enforced-in-review-and-by-architecture-tests) | 01, 11, 12 | planned; 99.9%, RPO and RTO out-of-scope for MVP |
| V-10 | FR-8, FR-10 (allocation); bordereaux none | ADR-0012 | [rules-and-rating.md](product/rules-and-rating.md#quota-share-allocation) | 09 | allocation planned; bordereaux out-of-scope |
| V-11 | all | all | [docs/architecture/](../docs/architecture/), [docs/adr/](../docs/adr/), [specs/contracts/](contracts/), [data-model.md](product/data-model.md) | every ticket keeps them current | verified (this documentation set) |
| P-1 | NFR-21, NFR-24 | ADR-0011 | [06-crosscutting.md](../docs/architecture/06-crosscutting.md#code-layout) | 01 | planned |
| P-2 | FR-7, FR-10.6, FR-11.3, FR-14, FR-15.3 | ADR-0005 | [06-crosscutting.md](../docs/architecture/06-crosscutting.md#idempotency) | 04, 07, 08, 09, 10 | planned |
| P-3 | FR-7.5, FR-8.6, FR-10.7, FR-15 | ADR-0005 | [events/catalog.md](contracts/events/catalog.md#delivery-and-consumption-rules) | 06, 07, 09 | planned |
| P-4 | FR-16 | ADR-0010 | [contracts/README.md](contracts/README.md#versioning-rules) | 01, 11 | planned |
| P-5 | FR-13 | ADR-0008 | [06-crosscutting.md](../docs/architecture/06-crosscutting.md#tenancy) | 01, 04, 09, 12 | planned |
| P-6 | NFR-24, NFR-28 | ADR-0001, 0009, 0014 | [05-deployment.md](../docs/architecture/05-deployment.md) | 00, 01 | planned |
| P-7 | all | all | this file, [AGENTS.md](../AGENTS.md#spec-driven-workflow) | every ticket | in-progress |

## Requirement to test

Test names start with the criterion id (`FR07_3_...`). Fill the Tests column as tests land; a requirement is `verified` when every criterion has a passing test.

| Requirement | Ticket | Test project | Tests | Status |
| --- | --- | --- | --- | --- |
| FR-1 Account and Applicant | 04 | Policy.Tests | | planned |
| FR-2 Policy schedule | 05 | Policy.Tests | | planned |
| FR-3 NAICS Extensions | 03, 05 | Rules.Tests, Policy.Tests | | planned |
| FR-4 Decision | 03 | Rules.Tests | | planned |
| FR-5 Rating | 02 | Rating.Tests | | planned |
| FR-6 Quote | 05 | Policy.Tests, Acceptance.Tests | | planned |
| FR-7 Bind | 07 | Policy.Tests, Acceptance.Tests | | planned |
| FR-8 Treaties | 06 | Carrier.Tests | | planned |
| FR-9 Treaty read model | 08, 11 | Ledger.Tests | | planned |
| FR-10 Booking | 09 | Ledger.Tests, Acceptance.Tests | | planned |
| FR-11 Booking Status | 10 | Policy.Tests, Acceptance.Tests | | planned |
| FR-12 Reconciliation | 10 | Policy.Tests, Ledger.Tests | | planned |
| FR-13 Tenancy | 01, 04, 09, 12 | Platform.Tests, Acceptance.Tests | FR13_missing_tenant_returns_tenant_required, FR13_unknown_tenant_returns_tenant_unknown | in-progress |
| FR-14 Idempotent commands | 04 | Platform.Tests, Policy.Tests | | planned |
| FR-15 Event publication and consumption | 06, 08, 11 | Platform.Tests, Ledger.Tests | | planned |
| FR-16 Versioned contracts | 01 and each service ticket | each service's contract tests | | planned |
| FR-17 Read endpoints | 04, 05, 06, 09 | per service | | planned |
| NFR-1 to NFR-4 Performance | 12 | Acceptance.Tests | | planned |
| NFR-5 to NFR-7 Resilience | 11 | Acceptance.Tests | | planned |
| NFR-8 Sync call resilience | 05 | Policy.Tests | | planned |
| NFR-9 Health | 01 | Acceptance.Tests | NFR09_health_live_and_ready_do_not_require_a_tenant, NFR09_health_ready_returns_200 | in-progress |
| NFR-10 Money precision | 02, 09 | Rating.Tests, Ledger.Tests | | planned |
| NFR-11 Ledger insert-only | 09 | Ledger.Tests | | planned |
| NFR-12 Reconstruction | 09, 10 | Ledger.Tests | | planned |
| NFR-13 Immutability | 06, 07 | Carrier.Tests, Policy.Tests | | planned |
| NFR-14 Timestamps and effective dates | 05, 06, 09 | per service | | planned |
| NFR-15 Correlation | 09 | Acceptance.Tests | | planned |
| NFR-16, NFR-17 Database isolation | 01 | Architecture.Tests, Acceptance.Tests | NFR16_each_database_resource_has_exactly_one_referencing_service, NFR17_login_from_one_instance_is_rejected_by_another | in-progress |
| NFR-18 Tenant isolation | 12 | Acceptance.Tests | | planned |
| NFR-19, NFR-20 Secrets, PII in logs | 01, 04 | review, Policy.Tests | | planned |
| NFR-21 Architecture rules | 01 | Architecture.Tests | NFR21_domain_projects_reference_the_bcl_only, NFR21_application_projects_reference_only_their_domain, NFR21_services_do_not_reference_each_other, NFR21_platform_does_not_reference_a_service_or_domain_type, NFR21_only_the_apphost_references_aspire_hosting | in-progress |
| NFR-22 Contract tests | 01 | each service | NFR22 via FR13_missing_tenant_returns_tenant_required (problem body against the operation schema) | in-progress |
| NFR-23 Unknown-field tolerance | 11 | Ledger.Tests | | planned |
| NFR-24 Image per service | 01 | PublishContainer build | | planned |
| NFR-25 to NFR-27 Observability | 01, 09 | review, Acceptance.Tests | | planned |
| NFR-28, NFR-29 Stack start, migrations | 01 | Acceptance.Tests | | planned |
| NFR-30 Runbook | 11 | review | | planned |

## Out of scope for the MVP

| Item | Source | Where it is recorded | First step when picked up |
| --- | --- | --- | --- |
| SSO federation, per-user authorisation | V-8 | ADR-0008 | OIDC at a gateway; tenant from token claim replaces the header |
| IVANS, EZLynx connectivity | V-8 | System context | Anti-corruption layer service consuming `policy-bound` |
| Property, loss history, credit/FCRA enrichment | V-8 | System context | Submission context with enrichment adapters |
| Payment gateway, billing | V-8 | System context | Billing service consuming `premium-booked` |
| Bordereaux | V-10 | System context | Bordereaux service built from ledger events alone |
| 99.9% uptime, RPO 15 min, RTO 4 hr | V-9 | NFR document | Azure SQL with PITR and zone redundancy, managed Kafka |
| Endorsements, cancellations | V-4, V-5 | data-model.md | New Policy Revision plus `Endorsement` premium transaction |
| Underwriter referral workbench | V-6 | user-flow.md | Referral queue in Policy, decision recorded on the revision |
