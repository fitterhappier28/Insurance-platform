---
status: accepted
---

# Ledger computes and stores allocation; lines are insert-only

Ledger is the financial record. At booking it writes the premium line per Exposure, the Factor Lines copied from the bound Rate Snapshot, and the Allocation Lines per Carrier, all in one transaction with the treaty version id. Nothing in the ledger is ever updated or deleted; a correction is a new transaction. Storing the allocation, rather than recomputing it from Carrier data on read, means a later treaty change cannot restate history and bordereaux can be built from the ledger alone.

## Consequences

- Rating Factors are stored twice in a policy's life: on the Policy Revision (quote record) and in the ledger (financial record). That copy is intended.
- Rounding remainders go to the Lead Carrier so Allocation Lines always sum to the premium exactly.
- Ledger's runtime SQL login is denied `UPDATE` and `DELETE` on ledger tables.
