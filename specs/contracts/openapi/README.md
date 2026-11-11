# OpenAPI contracts

| File | Service | Port |
| --- | --- | --- |
| [common.v1.yaml](common.v1.yaml) | shared components | n/a |
| [policy.v1.yaml](policy.v1.yaml) | Policy | 5101 |
| [rules.v1.yaml](rules.v1.yaml) | Rules | 5102 |
| [rating.v1.yaml](rating.v1.yaml) | Rating | 5103 |
| [carrier.v1.yaml](carrier.v1.yaml) | Carrier | 5104 |
| [ledger.v1.yaml](ledger.v1.yaml) | Ledger | 5105 |

Conventions, error codes and versioning rules: [../README.md](../README.md).

Policy calls Rules and Rating through small hand-written typed clients; contract tests check their requests and the responses they parse against `rules.v1.yaml` and `rating.v1.yaml`. Each service embeds its file from this folder at build time and serves it at `/openapi/v1.yaml`; there is no second, code-generated contract to drift. Contract tests send real requests and validate every response body against the file.
