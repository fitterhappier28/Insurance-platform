# 02: Rules and Rating

**Status:** in-progress

**Outcome:** Rules and Rating expose separate versioned APIs. Rules returns an eligibility Decision from Exposure attributes. Rating returns a deterministic decimal Rate Snapshot with factor-level steps. The sample rules and rates are explicitly illustrative.

**Depends on:** 01

**Requirements:** FR-3, FR-4, FR-5, NFR-2, NFR-10, NFR-22

**Done when:** contract and domain tests cover the reference fixture, unsupported NAICS, declines, referrals, and decimal rounding.
