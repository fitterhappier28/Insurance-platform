---
status: accepted
---

# Contract-first APIs with the major version in the URL

OpenAPI 3.1 files in `specs/contracts/openapi/` are written and reviewed before endpoints exist; services embed and serve those files rather than generating a contract from code, and contract tests validate live responses against them. The major version is in the path (`/v1`); minors are additive and recorded in `info.version`. Path versioning is visible in logs, gateways and partner documentation, and lets two majors run side by side during a deprecation window.

## Considered Options

- Header or media-type versioning. Cleaner URLs, harder to route and to see in logs.
- Code-first with generated OpenAPI. Faster to start; the contract drifts to whatever the code does.

## Consequences

- Request bodies reject unknown properties; response enums are documented as open so adding a value is not breaking.
- A breaking change needs `/v2` and a deprecation period, enforced by a diff check against the last released file.
