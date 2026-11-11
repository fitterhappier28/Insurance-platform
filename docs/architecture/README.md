# Architecture

This is a learning architecture for a Commercial Property quote-to-bind flow with auditable premium booking. The target has four bounded contexts: Policy, Rules, Rating, and Ledger.

| View | Question |
| --- | --- |
| [01-system-context.md](01-system-context.md) | Who uses the platform and why? |
| [02-containers.md](02-containers.md) | Which deployable services own which data? |
| [03-policy-components.md](03-policy-components.md) | How does Policy coordinate quote and bind? |
| [03-ledger-components.md](03-ledger-components.md) | How does Ledger record and verify premium? |
| [04-dynamic-quote-bind-book.md](04-dynamic-quote-bind-book.md) | How do synchronous and asynchronous steps fit together? |
| [05-deployment.md](05-deployment.md) | How does the local topology map to a future Azure deployment? |
| [06-crosscutting.md](06-crosscutting.md) | How are contracts, tenancy, idempotency, events and observability handled? |

`workspace.dsl` is the Structurizr source. Markdown pages are the human-readable architecture narrative. Keep both aligned when the model changes. The authoritative business scope is in [specs/product](../../specs/product/); API and event contracts are in [specs/contracts](../../specs/contracts/).
