# 05: Policy: quote a schedule

**What to build:** A broker submits a schedule (Locations, Buildings, Units, Coverages, Exposures with NAICS Extensions) and gets back a Quote: Policy Revision 1 with the Rules Decision and, unless declined, the Rate Snapshot.

**Blocked by:** 02, 03, 04

**Status:** ready-for-agent

**Requirements:** FR-2, FR-3.2, FR-3.4, FR-6, FR-17.1 (Quote read), NFR-8, NFR-14

**Read first:** [user-flow.md](../../product/user-flow.md), [data-model.md](../../product/data-model.md#commercial-hierarchy-policy-service), [ADR-0003](../../../docs/adr/0003-quote-is-a-policy-revision.md)

- [ ] Domain model for Policy, Policy Revision, Coverage, Location, Building, Unit, Exposure with the invariants in data-model.md; migrations added.
- [ ] Request refs are resolved; dangling or duplicate refs, an Exposure on a Building or Unit outside the schedule, term outside 1 to 18 months, or an effective date in the past return `422 validation-failed` (FR-2.2, FR-2.4).
- [ ] Reference scenario returns `201`, status `Quoted`, premium `1410.75`, four factor steps, ruleset and manual versions, and every schedule element with its ref and id (FR-6.4).
- [ ] Alternate flows A1 to A4 return the statuses, reasons and premiums in user-flow.md; Decline does not call Rating (FR-6.1, FR-6.5).
- [ ] Rules `422` is passed through as `422 validation-failed` with pointers mapped to the quote request (A5).
- [ ] `expiresAt` is 30 days after `quotedAt`, driven by `TimeProvider` (FR-6.6).
- [ ] Rules or Rating unavailable returns `503 dependency-unavailable` and stores nothing (FR-6.7); client has timeout, retry and circuit breaker (NFR-8).
- [ ] `GET /v1/quotes/{id}` returns the stored Quote; `404` across tenants.
- [ ] Extension payload is stored verbatim with its schema id (FR-3.4).
- [ ] Unit-level Exposure (subjectRef = a Unit) is accepted and rated with its Building's attributes.
- [ ] Rules and Rating clients' requests and parsed responses validate against their contracts.
- [ ] Idempotent replay of `POST /v1/quotes` creates one Policy.
