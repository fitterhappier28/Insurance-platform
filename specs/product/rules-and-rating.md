# Rules and rating

Status: proposed learning fixture. The scenario values, rates, factors, and eligibility thresholds below are invented examples for demonstrating deterministic Rules/Rating contracts. They are not actuarial guidance or an approved underwriting appetite.

Rules decides eligibility and never prices. Rating prices and never decides eligibility. Both are stateless: they load their published, immutable version files at start-up and keep no business data.

## Inputs

Policy flattens each Exposure into a rating and rules input. Building and Location attributes are copied onto the Exposure input; Unit Exposures take their Building's attributes.

| Field | Source | Values |
| --- | --- | --- |
| `exposureRef` | Exposure id | string |
| `coverageCode` | Coverage | `BLDG` |
| `limit` | Exposure | decimal string, 1 to 25,000,000 |
| `naicsCode` | Exposure | 6 digits |
| `construction` | Building | `Frame`, `JoistedMasonry`, `NonCombustible`, `MasonryNonCombustible`, `FireResistive` |
| `yearBuilt` | Building | 1800 to policy effective year |
| `protectionClass` | Location | 1 to 10 |
| `effectiveDate` | Policy Revision | date |

## Ruleset CP-RULES-2026.07

Rules are evaluated for every Exposure. The Decision for the Quote is the most severe outcome across Exposures: Decline over Refer over Straight-Through. All triggered reasons are returned.

| Code | Outcome | Condition |
| --- | --- | --- |
| `D-NAICS-UNSUPPORTED` | Decline | `naicsCode` not in {531110, 722511} |
| `D-PROTECTION-CLASS` | Decline | `protectionClass` is 10 |
| `R-BUILDING-AGE` | Refer | `yearBuilt` before 1980 |
| `R-LIMIT` | Refer | `limit` above 10,000,000 |
| `R-PROTECTION-CLASS` | Refer | `protectionClass` is 9 |

## Rate Manual CP-RATE-2026.07

Premium per Exposure:

```
premium = round2( (limit / 100) * baseRate * occupancyFactor * protectionClassFactor * buildingAgeFactor )
```

### Base rate per 100 of limit, by construction

| Construction | Base rate |
| --- | --- |
| Frame | 0.4000 |
| JoistedMasonry | 0.2500 |
| NonCombustible | 0.2200 |
| MasonryNonCombustible | 0.2000 |
| FireResistive | 0.1500 |

### Rating Factors, applied in this order

| Order | Factor code | Basis | Values |
| --- | --- | --- | --- |
| 1 | `OCCUPANCY` | NAICS code | 531110: 0.95; 722511: 1.35 |
| 2 | `PROTECTION_CLASS` | Location protection class | 1-3: 1.00; 4-6: 1.10; 7-8: 1.25; 9: 1.50 |
| 3 | `BUILDING_AGE` | Year built | 2010 or later: 1.00; 1980-2009: 1.08; before 1980: 1.20 |

Rating also prices risks Rules would decline, because it does not know the Decision; Policy never calls Rating after a Decline. A protection class of 10 or a NAICS code outside the manual returns `422 not-rateable`.

## Precision and rounding

- All arithmetic is `decimal`. No intermediate rounding of rates or factors.
- Each Exposure's premium is rounded to the cent, half away from zero.
- Written Premium is the sum of rounded Exposure premiums.
- Factor Lines record the premium after each step: `round2((limit / 100) * baseRate * product of factors up to this step)`. Each step is computed from the unrounded cumulative product, never from the previous rounded step, so the last step always equals the Exposure premium.

## Reference calculation

Reference scenario Exposure: limit 500,000, JoistedMasonry, protection class 5, built 1998, NAICS 531110.

| Step | Factor | Value | Premium after step |
| --- | --- | --- | --- |
| 0 | `BASE_RATE` | 0.2500 per 100 | 1,250.00 |
| 1 | `OCCUPANCY` | 0.95 | 1,187.50 |
| 2 | `PROTECTION_CLASS` | 1.10 | 1,306.25 |
| 3 | `BUILDING_AGE` | 1.08 | 1,410.75 |

Effective rate 0.28215 per 100. Written Premium 1,410.75.

Alternate flow A2 (built 1972): building age factor 1.20, premium 1,567.50.
