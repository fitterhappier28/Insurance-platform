---
status: accepted
---

# Account lives in the Policy service for the MVP

Account and Applicant are a separate aggregate from Policy but deploy inside the Policy service, sharing its database. In the MVP, every Account use is on the quote path and nothing else consumes Accounts, so a sixth service would add a network hop, a fourth SQL Server and an `account-created` event without proving anything new. Account is the first extraction candidate: when clearance, producer-of-record or another line of business needs Accounts, it moves out with its own database and publishes events Policy consumes.

## Consequences

- Policy code must not join Account tables into policy queries beyond reading the Account by id, so the later split stays mechanical.
