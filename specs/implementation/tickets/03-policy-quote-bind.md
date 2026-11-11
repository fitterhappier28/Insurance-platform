# 03: Policy quote and bind

**Status:** ready-for-agent

**Outcome:** Policy owns tenant-scoped Accounts, Applicants, schedules, Policy Revisions, Quotes, pinned snapshots, and bind state. Quote and Bind are commands; reads use Policy-owned query models. Bind is idempotent and commits the immutable revision with `policy-bound` in its outbox.

**Depends on:** 01, 02

**Requirements:** FR-1, FR-2, FR-6, FR-7, FR-13, FR-14, FR-16, FR-17, NFR-5, NFR-8, NFR-13, NFR-18

**Done when:** tests prove quote outcomes, pinned-version behavior, stale/expired quote handling, tenant isolation, replayed bind response, immutability, and atomic outbox persistence.
