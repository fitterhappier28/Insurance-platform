# 03: Policy quote and bind

**Status:** done

**Outcome:** Policy owns tenant-scoped Accounts, Applicants, schedules, Policy Revisions, Quotes, pinned snapshots, and bind state. Quote and Bind are commands; reads use Policy-owned query models. Bind is idempotent and commits the immutable revision with `policy-bound` in its outbox.

**Depends on:** 01, 02

**Requirements:** FR-1, FR-2, FR-6, FR-7, FR-13, FR-14, FR-16, FR-17, NFR-5, NFR-8, NFR-13, NFR-18

**Done when:** tests prove quote outcomes, pinned Ruleset and Rate Manual behavior (including unavailable versions and changed re-evaluation), expired Quote handling, tenant isolation, replayed bind response, immutability, and atomic outbox persistence.

**Evidence:** `FR07_QuoteFreshnessTests` proves expiry at bind time, unavailable pinned Ruleset/Rate Manual handling, changed re-evaluation becoming stale, and matching re-evaluation remaining bindable. The full non-Acceptance solution run passed 37/37 tests, and `Acceptance.Tests` passed 9/9. Acceptance coverage verifies quote outcomes, pinned versions in the Quote and `policy-bound` event, atomic outbox persistence, replayed bind response, bound-revision immutability, and tenant isolation.
