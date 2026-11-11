---
status: accepted
---

# Keep the learning flow to Policy, Rules, Rating, and Ledger

The learning scope demonstrates four responsibilities: Policy owns the commercial workflow and bound revision; Rules decides eligibility; Rating calculates premium; Ledger records bound premium immutably. Policy calls Rules and Rating over HTTP while quoting and binding. After bind, Ledger consumes the complete bound snapshot from Kafka.

Carrier/Treaty management and quota-share allocation are excluded. They are valuable insurance capabilities, but optional in the target role and introduce separate business rules (treaty selection, effective dating, allocation, rounding, and reconciliation). The core quote-to-bind flow can demonstrate contract-first design, domain ownership, CQRS, Kafka, transactional outbox/inbox, and financial audit without those concerns.

The stateful Policy and Ledger contexts own separate databases. Rules and Rating are stateless and retain only immutable versioned configuration. The decomposition is justified for this educational demonstration; a production architecture review would reconsider deployment boundaries against team ownership, operational cost, scaling, and availability requirements.

## Consequences

- The main event flow is `policy-bound` to Ledger, followed by `premium-booked` back to Policy.
- Ledger records premium and factor lines, with no Carrier allocation lines.
- Reinsurance-related vacancy capabilities remain a documented coverage gap rather than being implied by a placeholder service.
- Azure is a target deployment design only; production SLOs remain unaccepted pending workload and recovery analysis.
