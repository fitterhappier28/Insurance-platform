# 12: Walkthrough and end-to-end acceptance

**What to build:** One command walks through the whole user flow against the running stack, the acceptance suite covers every alternate flow and the latency targets, and the traceability matrix is complete.

**Blocked by:** 11

**Status:** ready-for-agent

**Requirements:** user flow main flow and A1 to A13, NFR-1 to NFR-4, NFR-18

**Read first:** [user-flow.md](../../product/user-flow.md), [traceability.md](../../traceability.md)

- [ ] `scripts/walkthrough.ps1` runs against the AppHost stack and walks the main flow with readable output: treaties, account, quote, bind, booking, ledger transaction with factor and allocation lines.
- [ ] Acceptance tests cover the main flow and every alternate flow A1 to A13 with the exact values in user-flow.md.
- [ ] Tenant isolation: every read endpoint of Policy, Carrier and Ledger returns `404` for another tenant's ids (NFR-18).
- [ ] Latency over 100 sequential warm requests: quote p95 under 500 ms (NFR-1), bind p95 under 500 ms (NFR-3), bind-to-Booked p95 under 5 s (NFR-4).
- [ ] One trace in the Aspire dashboard spans bind, relay, Ledger booking and Policy's booking status update (screenshot added to `docs/architecture/`).
- [ ] Every row in [traceability.md](../../traceability.md) has tests and status `verified`, or `out-of-scope` with its reason.
- [ ] README "Running locally" verified from a clean clone.
