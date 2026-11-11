# 05: Status, reconciliation, and Azure design

**Status:** ready-for-agent

**Outcome:** Policy consumes `premium-booked` into a Booking Status projection. Read APIs and reconciliation checks expose Pending bookings and Ledger arithmetic mismatches. A future Azure deployment design documents identity, network, database recovery, event operations, telemetry, CI/CD, infrastructure as code, and cost assumptions.

**Depends on:** 04

**Requirements:** FR-11, FR-12, FR-17, NFR-4, NFR-9, NFR-15, NFR-26, NFR-27, NFR-30

**Done when:** event-driven status is idempotent, reconciliation scenarios are demonstrated, and Azure options are recorded with assumptions and unresolved choices rather than implied as deployed.
