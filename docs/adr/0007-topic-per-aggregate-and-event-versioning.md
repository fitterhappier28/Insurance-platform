---
status: accepted
---

# One topic per aggregate; event types carry their own major version

The learning flow uses one topic per stateful aggregate stream (`clp.policy.v1` by `policyId`, `clp.ledger.v1` by `policyId`). This keeps events for one policy ordered while keeping the topic set small. Events use a CloudEvents envelope and JSON Schema per type; additive optional fields keep the type, and breaking changes introduce a new major event type. Consumers ignore unknown optional fields and safely skip unknown event types.

## Considered Options

- Topic per event type. Loses per-aggregate order across types.
- One platform topic. Every consumer reads everything; no independent retention.

## Consequences

- There is no ordering across topics. Consumers that need facts from two aggregates join on ids and use `aggregateSeq`, not arrival order.
- Schemas are validated in contract tests rather than by a schema registry in the MVP; a registry is the production step.
