# Contracts

Active flow: Policy, Rules, Rating, and Ledger. Reinsurance is not represented by contracts in this learning scope.

Contracts are written and reviewed before the code that implements them. They win over code when the two disagree.

| Folder | Contents | Dialect |
| --- | --- | --- |
| [openapi/](openapi/) | One OpenAPI file per service | OpenAPI 3.1, JSON Schema 2020-12 |
| [events/](events/) | Event catalog, CloudEvents envelope, one schema per event type | JSON Schema 2020-12 |

## HTTP conventions

| Header | Direction | Rule |
| --- | --- | --- |
| `X-Tenant-Id` | request | Required on every call. MVP values: `harbor`, `northstar` |
| `Idempotency-Key` | request | Required on every `POST` command, 1 to 64 characters. Not used on the side-effect-free calculations `POST /v1/decisions` and `POST /v1/rates` |
| `X-Correlation-Id` | both | Optional on request; generated if absent; echoed on response and propagated to events |
| `Idempotent-Replayed` | response | `true` when the response is a stored replay |
| `traceparent` | both | W3C trace context |

- Routes are `/v1/...`. The service serves its contract file unchanged at `/openapi/v1.yaml`.
- Request bodies reject unknown properties (`additionalProperties: false`). A command must not silently drop a field.
- Money and rates are JSON strings matching `^-?\d+(\.\d{1,4})?$` for money and `^\d+(\.\d{1,8})?$` for rates and factors.
- Dates are `YYYY-MM-DD`; timestamps are RFC 3339 UTC.
- Enums in responses may grow; clients must tolerate unknown values. This is stated in every spec where it applies.

## Error model

Every error is `application/problem+json`:

```json
{
  "type": "https://clp.example/problems/quote-not-bindable",
  "title": "Quote cannot be bound",
  "status": 409,
  "code": "quote-not-bindable",
  "detail": "Revision status is Referred.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [ { "pointer": "/exposures/0/limit", "code": "required" } ]
}
```

Clients branch on `code`, never on `title` or `detail`.

| Code | Status | Raised by |
| --- | --- | --- |
| `validation-failed` | 422 | all |
| `tenant-required` | 400 | all |
| `tenant-unknown` | 403 | all |
| `idempotency-key-required` | 400 | all `POST` commands |
| `idempotency-key-reused` | 422 | all `POST` commands |
| `idempotency-key-in-flight` | 409 | all `POST` commands |
| `not-found` | 404 | all |
| `quote-not-bindable` | 409 | Policy |
| `quote-expired` | 409 | Policy |
| `quote-stale` | 409 | Policy |
| `dependency-unavailable` | 503 | Policy |
| `not-rateable` | 422 | Rating |

## Versioning rules

The URL carries the major version (`/v1`). `info.version` in each OpenAPI file carries `major.minor.patch`. Event types carry their own major (`.v1`) in the type name.

| Change | HTTP API | Event |
| --- | --- | --- |
| Add an optional response field | Compatible, minor | Compatible, minor |
| Add an optional request field | Compatible, minor | n/a |
| Add a required request field | Breaking: `/v2` | n/a |
| Remove or rename a field | Breaking: `/v2` | Breaking: new type `.v2` |
| Change a field's type or format (including money string to number) | Breaking: `/v2` | Breaking: new type `.v2` |
| Change the meaning of a field or enum value | Breaking: `/v2` | Breaking: new type `.v2` |
| Add an enum value in a response | Compatible (clients tolerate unknown values) | Compatible; consumers park values they must act on but do not know |
| Tighten validation | Breaking | Breaking if producers already emit the old form |
| Change an error status code | Breaking | n/a |

When a major ships, the previous major stays available with `Deprecation` and `Sunset` headers for at least one release (internal) or 180 days (externally consumed). For events, the producer publishes both types during the window; the outbox keeps the original payloads.

A contract test compares each changed OpenAPI file against the last released one and fails the build on a breaking change without a new major.
