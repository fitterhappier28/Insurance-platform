# 02: Rating: price exposures

**What to build:** `POST /v1/rates` returns the premium and factor-level breakdown for each Exposure from Rate Manual `CP-RATE-2026.07`, with no stored state.

**Blocked by:** 01

**Status:** ready-for-agent

**Requirements:** FR-5, NFR-2, NFR-10

**Read first:** [rules-and-rating.md](../../product/rules-and-rating.md#rate-manual-cp-rate-202607), [rating.v1.yaml](../../contracts/openapi/rating.v1.yaml)

- [ ] Rate Manual `CP-RATE-2026.07` is a versioned file loaded at start-up; `GET /v1/rate-manuals` lists it with its effective date.
- [ ] Reference Exposure returns `1410.75` with steps `1250.00`, `1187.50`, `1306.25`, `1410.75` (FR-5.1, FR-5.2).
- [ ] Built 1972 returns `1567.50` (alternate flow A2).
- [ ] Every construction class, protection class band and building age band has a unit test against a hand-computed value.
- [ ] Multiple Exposures: total premium is the sum of rounded Exposure premiums.
- [ ] Protection class 10 or NAICS outside the manual returns `422 not-rateable` (FR-5.4).
- [ ] Unknown request property returns `422 validation-failed`.
- [ ] Pinned `rateManualVersion` is honoured; unknown version returns `422 validation-failed` (FR-5.5).
- [ ] No `double` or `float` in the Rating projects (architecture test, NFR-10).
- [ ] p95 under 100 ms over 200 in-process requests (NFR-2).
- [ ] Responses validate against `rating.v1.yaml`.
