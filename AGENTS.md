# AGENTS.md

Guidance for anyone, human or agent, changing this repository. Read this first, then [GLOSSARY.md](GLOSSARY.md).

## What this is

Commercial Lines Platform (CLP): a learning architecture for Commercial Property quote, bind, and premium booking across four contexts (Policy, Rules, Rating, Ledger). See [README.md](README.md).

## Spec-driven workflow

Specs lead, code follows. A change is done when the spec, the contract, the code, the tests and the traceability row agree.

1. Find or write the requirement in [specs/product/](specs/product/). New behaviour gets a new `FR-n` or `NFR-n`; never renumber existing ids.
2. Update the contract first: OpenAPI in [specs/contracts/openapi/](specs/contracts/openapi/) and event schemas in [specs/contracts/events/](specs/contracts/events/).
3. If the change is hard to reverse, surprising, and a real trade-off, add an ADR in [docs/adr/](docs/adr/). Update [docs/architecture/workspace.dsl](docs/architecture/workspace.dsl) and the matching view page when containers, relationships or components change.
4. Implement test-first. Test names start with the requirement id, for example `FR07_bind_replayed_with_same_key_returns_original_policy`.
5. Fill the requirement's row in [specs/traceability.md](specs/traceability.md) with the test names.
6. Set the ticket's `Status:` in [specs/implementation/tickets/](specs/implementation/tickets/).

Source-of-truth order when documents disagree: contracts, then product specs, then ADRs, then architecture views, then code. Fix the lower one, or raise the conflict.

`specs/prerequisites/` is local reference material. It is not committed and is never a source of truth.

## Guardrails (enforced in review and by architecture tests)

- A service reads and writes only its own database. No cross-service SQL, no shared tables, no shared connection strings.
- `*.Domain` projects reference nothing but the BCL. No EF Core, Kafka, ASP.NET or `Platform.*` packages in a domain project.
- `backend/src/Platform/*` holds technical code only. No domain types (policy, premium, exposure) in Platform.
- State change and outbox insert commit in one SQL transaction. Never publish to Kafka from a request handler.
- Every consumer is idempotent: inbox row plus effect in one transaction, plus a business unique key where one exists.
- Ledger rows are insert-only. Corrections are new transactions. No `UPDATE` or `DELETE` on ledger tables.
- Bound Policy Revisions and Premium Transactions are immutable.
- Money is `decimal` in C#, `decimal(19,4)` in SQL, a string in JSON. Never `double` or `float`. Rounding rules live in [specs/product/rules-and-rating.md](specs/product/rules-and-rating.md).
- Every request carries `X-Tenant-Id`; every row and event carries `tenantId`; every query filters by it.
- Every state-changing `POST` requires `Idempotency-Key` (calculations `/v1/decisions` and `/v1/rates` are exempt).
- Public HTTP routes are versioned `/v1/...` and match the OpenAPI file in `specs/contracts/openapi/`.
- Rules decides eligibility and never prices. Rating prices and never decides eligibility. Neither stores business outcomes.

## Conventions

- .NET 10, C# latest, nullable enabled, warnings as errors.
- Local topology (resources, wiring, ports, parameters) is defined only in `backend/src/AppHost`. Services read configuration and connection strings; they do not reference Aspire hosting packages.
- Service layout: `backend/src/Services/<Name>/<Name>.Domain`, `.Application`, `.Infrastructure`, `.Api`. Tests in `backend/tests/<Name>.Tests`.
- Minimal APIs with `Asp.Versioning`, problem details for every error.
- EF Core migrations live in `<Name>.Infrastructure`; each service migrates its own database on start.
- Integration tests use Testcontainers (SQL Server, Kafka). End-to-end tests in `tests/Acceptance.Tests` start the stack through `Aspire.Hosting.Testing`.
- Use the glossary terms in code, tests, tickets and docs. Avoid the synonyms it lists.

## Commands

```powershell
dotnet build backend/InsurancePlatform.sln
dotnet test backend/InsurancePlatform.sln --filter "Category!=Acceptance"
dotnet run --project backend/src/AppHost
dotnet test backend/tests/Acceptance.Tests
./scripts/walkthrough.ps1
```

## Agent skills

### Issue tracker

Tickets are markdown files in `specs/implementation/tickets/`. See `harness/issue-tracker.md`.

### Triage labels

Default five-role vocabulary as a `Status:` line. See `harness/triage-labels.md`.

### Domain docs

Single-context: root `GLOSSARY.md` and `docs/adr/`. See `harness/domain.md`.
