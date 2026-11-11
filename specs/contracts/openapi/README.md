# OpenAPI contracts

| File | Context | Local port |
| --- | --- | --- |
| [common.v1.yaml](common.v1.yaml) | Shared contract components | n/a |
| [policy.v1.yaml](policy.v1.yaml) | Policy | 5101 |
| [rules.v1.yaml](rules.v1.yaml) | Rules | 5102 |
| [rating.v1.yaml](rating.v1.yaml) | Rating | 5103 |
| [ledger.v1.yaml](ledger.v1.yaml) | Ledger | 5104 |

Policy calls Rules and Rating through typed clients. Contract tests validate requests and responses against these files. Each active service serves its own contract at `/openapi/v1.yaml`. Shared conventions and versioning are in [../README.md](../README.md).
