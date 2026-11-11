# NAICS Extensions

Industry-specific attributes an Exposure must carry for its NAICS code. Owned by Rules; stored verbatim by Policy (see [../../product/data-model.md](../../product/data-model.md#naics-extensions)).

| Schema id | NAICS | Status |
| --- | --- | --- |
| `naics/531110.v1` | 531110 Lessors of buildings | active |
| `naics/722511.v1` | 722511 Full-service restaurants | active |

Rules:

- The schema id is `naics/<code>.v<major>`. The id prefix must equal the Exposure's NAICS code.
- Extension payloads are commands: unknown properties are rejected.
- Adding an optional field is a minor change to the same schema. Adding a required field, removing a field or changing a type is a new major (`.v2`); Rules accepts both majors during the transition.
- Adding a NAICS code means adding a schema here, rules in the Ruleset and an occupancy factor in the Rate Manual. Policy needs no change.
