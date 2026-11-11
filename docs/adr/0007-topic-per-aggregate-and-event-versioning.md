---
status: accepted
---

# One topic per aggregate; event types carry their own major version

Each aggregate type has one topic keyed by aggregate id (`clp.policy.v1` by `policyId`, `clp.carrier.v1` by `treatyId`, `clp.ledger.v1` by `policyId`), so all events for one policy stay in order and a consumer subscribes to a business stream rather than to dozens of type topics. Events use a CloudEvents envelope with a JSON Schema per type; additive optional fields keep the type, and any breaking change publishes a new `.v2` type alongside `.v1` until consumers move. Consumers ignore unknown fields and skip unknown types.

## Considered Options

- Topic per event type. Loses per-aggregate order across types.
- One platform topic. Every consumer reads everything; no independent retention.

## Consequences

- There is no ordering across topics. Consumers that need facts from two aggregates join on ids and use `aggregateSeq`, not arrival order.
- Schemas are validated in contract tests rather than by a schema registry in the MVP; a registry is the production step.
