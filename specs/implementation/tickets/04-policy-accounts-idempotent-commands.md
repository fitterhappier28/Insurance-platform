# 04: Policy: accounts and idempotent commands

**What to build:** A broker creates and reads Accounts in their own tenant. Repeating a create with the same `Idempotency-Key` returns the original response and creates nothing new. This ticket brings Policy's database, the tenant filter and the shared idempotency mechanism into use.

**Blocked by:** 01

**Status:** ready-for-agent

**Requirements:** FR-1, FR-13.2, FR-13.3, FR-14, FR-17.1 (Account read), NFR-20

**Read first:** [06-crosscutting.md](../../../docs/architecture/06-crosscutting.md#idempotency), [data-model.md](../../product/data-model.md), [policy.v1.yaml](../../contracts/openapi/policy.v1.yaml)

- [ ] Policy DB migrations for Account, Applicant and the `messaging` tables, applied on start with the owner login; requests use the runtime login.
- [ ] `POST /v1/accounts` creates Account and Applicant and returns `201` with `Location` (FR-1.1).
- [ ] `GET /v1/accounts/{id}` returns it in the same tenant (FR-1.2) and `404 not-found` from `northstar` (FR-13.3).
- [ ] Bad FEIN returns `422 validation-failed` (FR-1.3).
- [ ] `Platform.Idempotency`: missing key `400`; replay with same body returns stored response and `Idempotent-Replayed: true` and one Account exists; same key different body `422 idempotency-key-reused`; concurrent same key `409 idempotency-key-in-flight`; simulated 5xx is not stored (FR-14.1 to FR-14.6).
- [ ] Same key used by `harbor` and `northstar` creates two independent Accounts (FR-14.2).
- [ ] Tenant-filter test: every Policy entity is tenant-filtered (FR-13.2).
- [ ] FEIN never appears in logs during these tests (NFR-20).
- [ ] Responses validate against `policy.v1.yaml`.
