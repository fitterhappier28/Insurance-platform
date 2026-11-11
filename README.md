# Commercial Lines Platform (CLP)

MVP of a Commercial Property quote-to-bind platform. A broker quotes and binds a policy for a commercial building; the bound premium is booked to an immutable Premium Ledger and allocated across reinsurance carriers under the tenant's quota-share treaty.

Five services, each owning its data:

| Service | Responsibility | Storage |
| --- | --- | --- |
| Policy | Account and policy hierarchy, quote, bind, booking status | own SQL Server |
| Rules | Eligibility: straight-through, refer, decline | stateless, versioned ruleset files |
| Rating | Price only, factor-level breakdown | stateless, versioned rate manual files |
| Carrier | Carriers, quota-share treaties and their versions | own SQL Server |
| Ledger | Immutable premium, factor and allocation lines | own SQL Server |

Services talk synchronously over versioned HTTP APIs (OpenAPI 3.1) on the quote path and asynchronously over Kafka (transactional outbox, idempotent inbox) everywhere else. Every request and event is tenant-scoped. The local stack is orchestrated by .NET Aspire.

## Where things are

| Need | Go to |
| --- | --- |
| Domain language | [GLOSSARY.md](GLOSSARY.md) |
| What we build and why | [specs/product/](specs/product/) |
| API and event contracts | [specs/contracts/](specs/contracts/) |
| Architecture (C4) | [docs/architecture/](docs/architecture/) |
| Decisions | [docs/adr/](docs/adr/) |
| Requirement to test mapping | [specs/traceability.md](specs/traceability.md) |
| Implementation plan and tickets | [specs/implementation/](specs/implementation/) |
| Rules for contributors and agents | [AGENTS.md](AGENTS.md) |

## Repository layout

```
docs/architecture/     C4 model (Structurizr DSL) and rendered views
docs/adr/              architecture decision records
specs/product/         user flow, requirements, data model, rules and rating
specs/contracts/       OpenAPI, event schemas, NAICS extension schemas
specs/implementation/  plan and tickets
backend/               solution, services, platform libraries, tests
  src/AppHost/         .NET Aspire AppHost: the whole local topology as code
  src/Platform/        shared technical libraries (no domain code)
  src/Services/<Name>/ <Name>.Domain, .Application, .Infrastructure, .Api
  tests/               per-service tests and end-to-end acceptance tests
harness/               agent operating notes (issue tracker, triage, domain)
scripts/               walkthrough and operational scripts
```

## Running locally

Requires the .NET 10 SDK and Docker Desktop (WSL 2 backend) with about 10 GB of memory available.

```powershell
dotnet run --project backend/src/AppHost
# in another shell, once the dashboard shows all resources healthy:
./scripts/walkthrough.ps1
```

The AppHost prints the Aspire dashboard URL (traces, metrics, logs, resource state). Service ports are fixed:

| Resource | URL |
| --- | --- |
| Policy API | http://localhost:5101/swagger |
| Rules API | http://localhost:5102/swagger |
| Rating API | http://localhost:5103/swagger |
| Carrier API | http://localhost:5104/swagger |
| Ledger API | http://localhost:5105/swagger |
| Kafka UI | http://localhost:8085 |
| C4 model (Structurizr) | http://localhost:8086 |

Secrets (SQL Server passwords) are Aspire parameters; on first run they are generated into the AppHost's user secrets, never into the repository.

`dotnet run --project backend/src/AppHost` starts the five services, three SQL Servers, Kafka, Kafka UI and Structurizr. Docker Desktop has to be running. Walkthrough script arrives with ticket 12.
