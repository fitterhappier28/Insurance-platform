---
status: accepted
---

# .NET Aspire AppHost orchestrates the local stack

The local stack is defined in code by an Aspire AppHost (`backend/src/AppHost`): Policy, Rules, Rating, and Ledger; SQL Server for the two stateful contexts; and Kafka. Aspire gives service discovery, health-gated start order, secret parameters, and `Aspire.Hosting.Testing` so acceptance tests can start the same stack as a developer. This is a local development choice, not the production deployment model.

## Considered Options

- Keep Docker Compose with per-service data networks. Gives network-level isolation, but a second definition of the topology next to the .NET code, and no integrated dashboard or test host.
- Docker Compose. Viable, but would create a second local topology beside the .NET AppHost and would require separate integration-test wiring.

## Consequences

- Aspire places all resources on one container network, so a database is reachable by name from any container. Isolation is kept by other means, each tested: a service gets a connection string only for its own database (AppHost wiring), each database has its own credentials, and an architecture test fails if the AppHost references a service to a database it does not own.
- The AppHost and a ServiceDefaults extension (in `Platform.Web`) are the only places that know about the dashboard, OTLP and resource names. Service code reads `ConnectionStrings:*` and `Kafka` configuration and does not depend on Aspire.
- Structurizr Lite is run by the AppHost as a container resource, so architecture views come up with the stack.
- Requires the .NET 10 SDK and a container runtime (Docker Desktop). Aspire's CLI is optional.
- Production hosting is a separate concern (ADR-0009 and the Azure mapping in the deployment page).
