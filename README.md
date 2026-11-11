# Commercial Lines Platform (learning project)

A focused Commercial Property quote-to-bind architecture exercise. It demonstrates domain boundaries, separate eligibility and pricing, versioned contracts, idempotent commands, Kafka integration through transactional outbox/inbox, and an immutable premium record with factor-level traceability.

## Contexts and ownership

| Context | Responsibility | Data |
| --- | --- | --- |
| Policy | Account and policy schedule, quote, bind, Booking Status | Own SQL database |
| Rules | Eligibility decisions | Immutable versioned rulesets; no quote data |
| Rating | Price and factor explanation | Immutable versioned rate manuals; no quote data |
| Ledger | Record and query bound premium | Own append-only SQL database |

Policy calls Rules and Rating over HTTP during quote and bind. After bind, Policy publishes `policy-bound` through its transactional outbox. Ledger consumes it from Kafka and records the financial transaction. See [the user flow](specs/product/user-flow.md) and [architecture views](docs/architecture/README.md).

This decomposition is intentionally used to demonstrate the target role's technical skills. It is not a claim that every context must be an independently deployed production microservice.

## Repository map

| Area | Contents |
| --- | --- |
| [specs/product](specs/product/) | Flow, functional/non-functional requirements, domain data, rating rules |
| [specs/contracts](specs/contracts/) | OpenAPI contracts and event schemas |
| [docs/architecture](docs/architecture/) | C4 views, deployment mapping, operations notes |
| [docs/adr](docs/adr/) | Architecture decisions and superseded decisions |
| [specs/traceability.md](specs/traceability.md) | Vacancy skills mapped to scope and evidence |
| [specs/implementation](specs/implementation/) | Learning implementation plan |
| [GLOSSARY.md](GLOSSARY.md) | Domain vocabulary |

## Technology direction

The code direction is .NET, SQL Server, Kafka-compatible messaging, OpenAPI 3.1, JSON Schema, and OpenTelemetry. Aspire is for local orchestration. [The deployment view](docs/architecture/05-deployment.md) maps the design to possible Azure services and identifies decisions still requiring validation; no production Azure environment is implied.

See [AGENTS.md](AGENTS.md) for repository workflow and commands.

## Run locally

Requires the .NET 10 SDK and Docker Desktop. Start the local topology with:

```powershell
dotnet run --project backend/src/AppHost
```

| Context | Local API |
| --- | --- |
| Policy | `http://localhost:5101/swagger` |
| Rules | `http://localhost:5102/swagger` |
| Rating | `http://localhost:5103/swagger` |
| Ledger | `http://localhost:5104/swagger` |
| Kafka UI | `http://localhost:8085` |
| Structurizr | `http://localhost:8086` |
