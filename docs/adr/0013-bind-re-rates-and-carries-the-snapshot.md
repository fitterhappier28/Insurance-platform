---
status: accepted
---

# Bind re-runs Rules and Rating; policy-bound carries the full Rate Snapshot

Bind calls Rules and Rating again and refuses with `quote-stale` if the Decision, any reason or the premium changed since the Quote, so nothing is bound on a price the current manual would not produce. The resulting Rate Snapshot travels inside `policy-bound`, so Ledger books exactly what was bound without calling Policy or Rating, and the event alone is enough to reconstruct the premium.

## Considered Options

- Bind on the stored quote snapshot without re-rating. Faster, but a quote priced on a withdrawn manual could bind.
- Ledger re-rates on consumption. Puts a synchronous dependency on the money path and can disagree with what the broker saw.

## Consequences

- Bind depends on Rules and Rating being available (`503` otherwise); booking does not.
- `policy-bound` is larger than a thin notification event; that is deliberate.
