# Architecture

C4 documentation for the Commercial Lines Platform.

The model lives in one file, [workspace.dsl](workspace.dsl) (Structurizr DSL). Every view below is generated from it, and each page carries a Mermaid copy so it renders in GitHub and in the IDE without tooling. When the model changes, edit `workspace.dsl` first, then the Mermaid copy on the matching page.

| Page | C4 level | Question it answers |
| --- | --- | --- |
| [01-system-context.md](01-system-context.md) | 1 Context | Who uses the platform, what does it connect to |
| [02-containers.md](02-containers.md) | 2 Containers | Which services, which databases, who owns what data, how they talk |
| [03-components.md](03-components.md) | 3 Components | Inside Policy and Ledger, the two services with the most logic |
| [04-dynamic-quote-bind-book.md](04-dynamic-quote-bind-book.md) | Dynamic | The reference scenario end to end |
| [05-deployment.md](05-deployment.md) | Deployment | Aspire AppHost topology and database isolation |
| [06-crosscutting.md](06-crosscutting.md) | n/a | Tenancy, idempotency, outbox and inbox, versioning, observability, code layout |
| [operations.md](operations.md) | n/a | Runbook: replay, dead-letter re-drive, reconciliation |

Decisions behind the shape are in [../adr/](../adr/). Requirements are in [../../specs/product/](../../specs/product/).

## Viewing the Structurizr model

```powershell
dotnet run --project src/AppHost
start http://localhost:8086
```

The AppHost runs Structurizr Lite as the `structurizr` resource. It reads `docs/architecture/workspace.dsl` and stores diagram layout in `workspace.json` next to it. Commit both.
