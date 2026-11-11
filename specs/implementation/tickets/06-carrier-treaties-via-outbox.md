# 06: Carrier: treaties published through the outbox

**What to build:** A treaty administrator creates a Treaty and publishes immutable Treaty Versions; each publication reaches Kafka as `treaty-version-published` through the transactional outbox. This ticket introduces the outbox writer, relay and CloudEvents envelope in `Platform.Messaging`.

**Blocked by:** 04

**Status:** ready-for-agent

**Requirements:** FR-8, FR-15.1, FR-15.2, FR-15.6, FR-17.2, NFR-13

**Read first:** [events catalog](../../contracts/events/catalog.md), [treaty-version-published.v1.schema.json](../../contracts/events/treaty-version-published.v1.schema.json), [carrier.v1.yaml](../../contracts/openapi/carrier.v1.yaml), [ADR-0005](../../../docs/adr/0005-transactional-outbox-and-inbox.md)

- [ ] Carrier DB migrations; `RENRE` and `ARCHRE` seeded; `GET /v1/carriers` lists them (FR-8.1).
- [ ] `POST /v1/treaties` and `GET /v1/treaties[/{id}]` per contract, tenant-scoped (FR-8.2, FR-8.7).
- [ ] `POST /v1/treaties/{id}/versions` publishes `HBR-CP-QS-2026.07` (70/30) for `harbor` and `NST-CP-QS-2026.07` (100 Arch Re) for `northstar`.
- [ ] Shares not summing to 100, unknown carrier, more than 4 decimals or non-positive share return `422 validation-failed` (FR-8.3).
- [ ] Duplicate effective date or version code returns `409 treaty-version-conflict` (FR-8.5).
- [ ] No API or code path updates a published version (FR-8.4, NFR-13).
- [ ] `Platform.Messaging` outbox: the version and its outbox row commit in one transaction; a forced failure after the version insert leaves neither (FR-8.6).
- [ ] Relay publishes to `clp.carrier.v1` keyed by `treatyId`, in `Sequence` order, then marks rows published; a crash between publish and mark re-publishes (FR-15.2).
- [ ] Published payload validates against the envelope and `treaty-version-published.v1` schemas, with `tenantid`, `correlationid`, `aggregateSeq` (FR-15.6).
- [ ] Idempotent replay of publish creates one version and one outbox row.
