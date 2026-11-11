---
status: accepted
---

# .NET Aspire AppHost orchestrates the local stack

The local stack is defined in code by an Aspire AppHost (`backend/src/AppHost`), not by a Compose file: five service projects, three SQL Server containers, Kafka and Kafka UI, with the Aspire dashboard for traces, metrics, logs and resource state. Aspire gives service discovery, health-gated start order, secrets as parameters, and `Aspire.Hosting.Testing` so acceptance tests start the same stack the developer runs. This supersedes the private-Docker-network clause of [ADR-0001](0001-database-server-per-stateful-service.md); separate SQL Server instances per stateful service stay.

## Considered Options

- Keep Docker Compose with per-service data networks. Gives network-level isolation, but a second definition of the topology next to the .NET code, and no integrated dashboard or test host.
- Aspire for development, generated Compose for anything else. Possible later (`aspire publish`); not needed for the MVP.

## Consequences

- Aspire places all resources on one container network, so a database is reachable by name from any container. Isolation is kept by other means, each tested: a service gets a connection string only for its own database (AppHost wiring), each database has its own credentials, and an architecture test fails if the AppHost references a service to a database it does not own.
- The AppHost and a ServiceDefaults extension (in `Platform.Web`) are the only places that know about the dashboard, OTLP and resource names. Service code reads `ConnectionStrings:*` and `Kafka` configuration and does not depend on Aspire.
- Structurizr Lite is run by the AppHost as a container resource, so architecture views come up with the stack.
- Requires the .NET 10 SDK and a container runtime (Docker Desktop). Aspire's CLI is optional.
- Production hosting is a separate concern (ADR-0009 and the Azure mapping in the deployment page).
