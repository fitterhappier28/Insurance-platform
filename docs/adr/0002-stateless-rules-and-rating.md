---
status: accepted
---

# Rules and Rating are stateless and load versioned files

Rules decides eligibility and never prices; Rating prices and never decides eligibility. Neither stores a business outcome: Policy keeps the Decision and Rate Snapshot on the Policy Revision, and Ledger keeps the booked Factor Lines. Rulesets and Rate Manuals are published, immutable, versioned files baked into each service image, so neither service has a database and both scale horizontally with no coordination.

## Considered Options

- Store rulesets and manuals in a database per service with an admin API. Needed once actuaries publish without a deploy; overkill while one version exists.
- One combined underwriting service. Rejected: mixes price with eligibility and makes "why was this declined" depend on pricing code.

## Consequences

- Publishing a new manual or ruleset is a release of that service. The version id travels with every result so old quotes stay reproducible.
- Policy orchestrates the two calls; it is the only place where the Decision and the price meet.
