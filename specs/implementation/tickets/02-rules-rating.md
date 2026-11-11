# 02: Rules and Rating

**Status:** ready-for-agent

**Outcome:** Rules and Rating expose separate versioned APIs. Rules validates NAICS extension data and returns an eligibility Decision. Rating returns a deterministic decimal Rate Snapshot with factor-level steps. The sample rules and rates are explicitly illustrative.

**Depends on:** 01

**Requirements:** FR-3, FR-4, FR-5, NFR-2, NFR-10, NFR-22

**Done when:** contract and domain tests cover the reference fixture, invalid extensions, declines, referrals, and decimal rounding.
