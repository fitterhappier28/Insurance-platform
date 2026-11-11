# 03: Rules: decide eligibility

**What to build:** `POST /v1/decisions` validates each Exposure's NAICS Extension and returns Straight-Through, Refer or Decline with every triggered reason from Ruleset `CP-RULES-2026.07`, with no stored state.

**Blocked by:** 01

**Status:** ready-for-agent

**Requirements:** FR-3.1 to FR-3.3, FR-4

**Read first:** [rules-and-rating.md](../../product/rules-and-rating.md#ruleset-cp-rules-202607), [naics/](../../contracts/naics/), [rules.v1.yaml](../../contracts/openapi/rules.v1.yaml)

- [ ] Ruleset and NAICS schemas `531110.v1`, `722511.v1` are loaded from versioned files; `GET /v1/rulesets` lists the ruleset with its schemas.
- [ ] Reference scenario returns `StraightThrough` with no reasons.
- [ ] Each reason code in the ruleset table has a test that triggers it alone and asserts the outcome.
- [ ] Mixed Exposures: Decline beats Refer beats Straight-Through; all reasons are returned with their `exposureRef` (FR-4.2).
- [ ] Extension missing a required field, with an unknown field, or with a wrong type returns `422 validation-failed` with a JSON pointer per error (FR-3.3, A5).
- [ ] Schema id whose NAICS prefix does not match the Exposure's NAICS code returns `422 validation-failed` (FR-3.2).
- [ ] Unsupported NAICS (447110) returns `Decline` with `D-NAICS-UNSUPPORTED`, not a validation error.
- [ ] Pinned `rulesetVersion` is honoured (FR-4.4).
- [ ] Responses validate against `rules.v1.yaml`.
