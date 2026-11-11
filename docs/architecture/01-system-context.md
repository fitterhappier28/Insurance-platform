# System context

```mermaid
flowchart LR
  Broker[Broker] -->|Quote and bind| CLP[Commercial Lines Platform]
  Finance[Finance analyst] -->|Inspect booked premium| CLP
  CLP -->|Future: identity and tenant claims| Identity[Entra ID / OIDC]
  CLP -->|Future: agency integrations| AMS[AMS / clearinghouses]
```

The learning scope demonstrates the Commercial Property path from schedule submission through quote, bind, and auditable premium booking. External identity and agency integrations are future concerns, represented in the Azure and integration architecture rather than implemented in the MVP.
