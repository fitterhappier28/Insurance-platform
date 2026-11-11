# 04: Ledger booking

**Status:** ready-for-agent

**Outcome:** Ledger consumes `policy-bound` from Kafka, stores Inbox receipt and an immutable Premium Transaction with premium and factor lines, and publishes `premium-booked` through its outbox.

**Depends on:** 01, 03

**Requirements:** FR-10, FR-15, NFR-3, NFR-7, NFR-11, NFR-12, NFR-15, NFR-23

**Done when:** tests cover duplicate deliveries, same business fact with a new event id, exact factor reconstruction, tenant isolation, and failure/recovery without duplicate booking.
