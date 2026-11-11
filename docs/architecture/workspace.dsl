workspace "Commercial Lines Platform" "Learning architecture for Commercial Property quote, bind and premium booking." {
  !identifiers hierarchical

  model {
    broker = person "Broker" "Creates quotes and binds eligible policies."
    finance = person "Finance analyst" "Inspects booked premium and its rating-factor history."
    platform = softwareSystem "Commercial Lines Platform" "Commercial Property quote-to-bind and auditable premium booking." {
      policy = container "Policy Service" "Owns Account, Applicant, policy schedule, quote and bind workflow." ".NET / ASP.NET Core" {
        policyApi = component "Policy API" "Versioned quote, bind and read contracts." "HTTP"
        workflow = component "Quote and bind workflow" "Coordinates Rules and Rating; commits bound revision and outbox atomically." "Application"
        policyStore = component "Policy persistence" "Owns policy state, booking status, idempotency, inbox and outbox." "SQL"
      }
      rules = container "Rules Service" "Decides eligibility using versioned rulesets." ".NET / ASP.NET Core"
      rating = container "Rating Service" "Prices Exposures and returns factor-level Rate Snapshots from versioned manuals." ".NET / ASP.NET Core"
      ledger = container "Ledger Service" "Appends immutable premium transactions from bound snapshots." ".NET / ASP.NET Core" {
        ledgerApi = component "Ledger API" "Reads premium transactions and reconciliation results." "HTTP"
        booking = component "Booking consumer" "Deduplicates policy-bound and appends financial records atomically." "Kafka consumer"
        ledgerStore = component "Ledger persistence" "Owns immutable premium lines, factor lines, inbox and outbox." "SQL"
      }
      policyDb = container "Policy Database" "Policy-owned state." "SQL Server" "Database"
      ledgerDb = container "Ledger Database" "Ledger-owned append-only financial records." "SQL Server" "Database"
      kafka = container "Kafka" "Carries policy-bound and premium-booked events." "Kafka" "Event Bus"
    }

    broker -> platform.policy.policyApi "Requests quote, binds policy, reads status" "HTTPS/JSON"
    finance -> platform.ledger.ledgerApi "Reads premium transactions" "HTTPS/JSON"
    platform.policy.policyApi -> platform.policy.workflow "Commands and queries"
    platform.policy.workflow -> platform.rules "Decision request" "HTTP/JSON"
    platform.policy.workflow -> platform.rating "Rate request" "HTTP/JSON"
    platform.policy.workflow -> platform.policy.policyStore "Persist quote or bind"
    platform.policy.policyStore -> platform.policyDb "Own database" "SQL"
    platform.policy.policyStore -> platform.kafka "Outbox relay: policy-bound" "Kafka"
    platform.kafka -> platform.ledger.booking "policy-bound" "Kafka"
    platform.ledger.booking -> platform.ledger.ledgerStore "Append transaction and inbox"
    platform.ledger.ledgerStore -> platform.ledgerDb "Own database" "SQL"
    platform.ledger.ledgerStore -> platform.kafka "Outbox relay: premium-booked" "Kafka"
    platform.kafka -> platform.policy.policyStore "premium-booked; update Booking Status" "Kafka"
    platform.ledger.ledgerApi -> platform.ledger.ledgerStore "Read transactions"
  }

  views {
    systemContext platform "SystemContext" {
      include *
      autolayout lr
    }
    container platform "Containers" {
      include *
      autolayout lr
    }
    component platform.policy "PolicyComponents" {
      include *
      autolayout lr
    }
    component platform.ledger "LedgerComponents" {
      include *
      autolayout lr
    }
    styles {
      element "Person" { shape person }
      element "Database" { shape cylinder }
      element "Event Bus" { shape pipe }
    }
  }
}
