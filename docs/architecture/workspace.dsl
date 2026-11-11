workspace "Commercial Lines Platform" "Commercial Property quote, bind and premium booking. MVP." {

    !identifiers hierarchical

    model {
        broker = person "Broker" "Requests quotes and binds Commercial Property policies on behalf of an applicant."
        treatyAdmin = person "Treaty administrator" "Maintains the tenant's quota-share treaties."
        financeAnalyst = person "Finance analyst" "Inspects booked premium and carrier allocation."

        idp = softwareSystem "Identity provider" "SSO federation. Planned: the MVP trusts X-Tenant-Id set at the gateway boundary." "External,Planned"
        ams = softwareSystem "Agency management systems" "IVANS and EZLynx policy download. Planned." "External,Planned"
        reinsurers = softwareSystem "Reinsurance carriers" "Renaissance Re and Arch Re. Receive bordereaux. Planned." "External,Planned"

        clp = softwareSystem "Commercial Lines Platform" "Quotes, binds and books Commercial Property premium, per tenant." {

            policy = container "Policy Service" "Account to Exposure hierarchy, quote orchestration, bind, booking status, reconciliation." ".NET 10, ASP.NET Core" "Service" {
                api = component "Policy HTTP API" "Versioned /v1 endpoints; tenant and idempotency middleware; problem details." "ASP.NET Core Minimal APIs"
                quote = component "Quote use case" "Builds Policy Revision 1 from the schedule, calls Rules then Rating, stores the Decision and Rate Snapshot." "Application"
                bind = component "Bind use case" "Re-runs Rules and Rating, assigns the policy number, binds the revision, enqueues policy-bound." "Application"
                bookingConsumer = component "Booking status consumer" "Applies premium-booked to the bound revision." "Application, Kafka consumer"
                reconciliation = component "Reconciliation query" "Lists bound revisions still Pending booking." "Application"
                domain = component "Policy domain model" "Account, Policy, Policy Revision and the Location, Building, Unit, Coverage, Exposure hierarchy; immutability of bound revisions." "Domain"
                rulesClient = component "Rules client" "Typed client for rules.v1 with timeout, retry and circuit breaker." "HttpClient"
                ratingClient = component "Rating client" "Typed client for rating.v1 with timeout, retry and circuit breaker." "HttpClient"
                persistence = component "Policy persistence" "DbContext with tenant query filters; outbox, inbox and idempotency tables." "EF Core"
                relay = component "Outbox relay" "Publishes unsent outbox rows in order, then marks them published." "Platform.Messaging"
            }
            policyDb = container "Policy DB" "Accounts, policies, revisions, decisions, rate snapshots; outbox, inbox, idempotency." "SQL Server 2022" "Database"

            rules = container "Rules Service" "Eligibility decisions and NAICS Extension validation. Stateless; ruleset files in the image." ".NET 10, ASP.NET Core" "Service"
            rating = container "Rating Service" "Price with factor-level breakdown. Stateless; rate manual files in the image." ".NET 10, ASP.NET Core" "Service"

            carrier = container "Carrier Service" "Carriers, quota-share treaties, immutable effective-dated treaty versions." ".NET 10, ASP.NET Core" "Service"
            carrierDb = container "Carrier DB" "Carriers, treaties, treaty versions and shares; outbox, idempotency." "SQL Server 2022" "Database"

            ledger = container "Ledger Service" "Treaty read model, immutable premium ledger, quota-share allocation, self-check." ".NET 10, ASP.NET Core" "Service" {
                api = component "Ledger HTTP API" "Read endpoints for transactions and the reconciliation self-check." "ASP.NET Core Minimal APIs"
                treatyConsumer = component "Treaty consumer" "Applies treaty-version-published to the treaty read model." "Application, Kafka consumer"
                policyBoundConsumer = component "Policy-bound consumer" "Hands policy-bound to the booking use case; retries, then dead-letters." "Application, Kafka consumer"
                booking = component "Booking use case" "Selects the effective Treaty Version and appends the Premium Transaction with premium, factor and allocation lines." "Application"
                domain = component "Ledger domain model" "Premium Transaction, lines, allocation and rounding rules; insert-only." "Domain"
                persistence = component "Ledger persistence" "DbContext; insert-only ledger tables, treaty read model, inbox, outbox." "EF Core"
                relay = component "Outbox relay" "Publishes premium-booked." "Platform.Messaging"
            }
            ledgerDb = container "Ledger DB" "Premium transactions and lines (insert-only), treaty read model, inbox, outbox." "SQL Server 2022" "Database"

            kafka = container "Kafka" "Topics clp.policy.v1, clp.carrier.v1, clp.ledger.v1 and dead-letter topics." "Apache Kafka 3.9, KRaft" "EventLog"
        }

        # People
        broker -> clp.policy.api "Creates accounts, requests quotes, binds" "HTTPS/JSON"
        treatyAdmin -> clp.carrier "Creates treaties, publishes treaty versions" "HTTPS/JSON"
        financeAnalyst -> clp.ledger.api "Reads premium transactions and self-check" "HTTPS/JSON"
        broker -> idp "Signs in" "OIDC" "Planned"

        # Policy internals
        clp.policy.api -> clp.policy.quote "Create quote"
        clp.policy.api -> clp.policy.bind "Bind quote"
        clp.policy.api -> clp.policy.reconciliation "Unbooked report"
        clp.policy.quote -> clp.policy.rulesClient "Decide"
        clp.policy.quote -> clp.policy.ratingClient "Rate"
        clp.policy.quote -> clp.policy.domain "Builds revision"
        clp.policy.quote -> clp.policy.persistence "Saves revision and policy-quoted outbox row"
        clp.policy.bind -> clp.policy.rulesClient "Re-decide"
        clp.policy.bind -> clp.policy.ratingClient "Re-rate"
        clp.policy.bind -> clp.policy.domain "Binds revision"
        clp.policy.bind -> clp.policy.persistence "Saves bound revision and policy-bound outbox row in one transaction"
        clp.policy.bookingConsumer -> clp.policy.persistence "Sets Booking Status with inbox row"
        clp.policy.reconciliation -> clp.policy.persistence "Queries Pending bookings"
        clp.policy.rulesClient -> clp.rules "POST /v1/decisions" "HTTP/JSON"
        clp.policy.ratingClient -> clp.rating "POST /v1/rates" "HTTP/JSON"
        clp.policy.persistence -> clp.policyDb "Reads and writes" "TDS"
        clp.policy.relay -> clp.policyDb "Reads unsent outbox rows" "TDS"
        clp.policy.relay -> clp.kafka "Publishes policy-quoted, policy-bound to clp.policy.v1" "Kafka"
        clp.policy.bookingConsumer -> clp.kafka "Consumes premium-booked from clp.ledger.v1" "Kafka"

        # Carrier
        clp.carrier -> clp.carrierDb "Reads and writes; outbox" "TDS"
        clp.carrier -> clp.kafka "Publishes treaty-version-published to clp.carrier.v1" "Kafka"

        # Ledger internals
        clp.ledger.api -> clp.ledger.persistence "Reads transactions"
        clp.ledger.treatyConsumer -> clp.kafka "Consumes treaty-version-published from clp.carrier.v1" "Kafka"
        clp.ledger.treatyConsumer -> clp.ledger.persistence "Stores treaty version with inbox row"
        clp.ledger.policyBoundConsumer -> clp.kafka "Consumes policy-bound from clp.policy.v1; dead-letters to clp.policy.v1.ledger.dlt" "Kafka"
        clp.ledger.policyBoundConsumer -> clp.ledger.booking "Book"
        clp.ledger.booking -> clp.ledger.domain "Builds transaction and allocation"
        clp.ledger.booking -> clp.ledger.persistence "Appends transaction, inbox and premium-booked outbox rows in one transaction"
        clp.ledger.persistence -> clp.ledgerDb "Inserts and reads" "TDS"
        clp.ledger.relay -> clp.ledgerDb "Reads unsent outbox rows" "TDS"
        clp.ledger.relay -> clp.kafka "Publishes premium-booked to clp.ledger.v1" "Kafka"

        # Planned integrations
        clp -> ams "Policy download" "IVANS / EZLynx" "Planned"
        clp -> reinsurers "Monthly bordereaux" "SFTP" "Planned"
        clp -> idp "Validates tokens" "OIDC" "Planned"

        local = deploymentEnvironment "Local" {
            deploymentNode "Developer workstation" ".NET 10 SDK, Docker Desktop" "" {
                deploymentNode "Aspire AppHost" "dotnet run --project backend/src/AppHost; also hosts the Aspire dashboard" ".NET Aspire" {
                    deploymentNode "policy-api" "port 5101" ".NET process" {
                        containerInstance clp.policy
                    }
                    deploymentNode "rules-api" "port 5102" ".NET process" {
                        containerInstance clp.rules
                    }
                    deploymentNode "rating-api" "port 5103" ".NET process" {
                        containerInstance clp.rating
                    }
                    deploymentNode "carrier-api" "port 5104" ".NET process" {
                        containerInstance clp.carrier
                    }
                    deploymentNode "ledger-api" "port 5105" ".NET process" {
                        containerInstance clp.ledger
                    }
                }
                deploymentNode "Docker containers" "Managed by Aspire" "Docker Desktop" {
                    deploymentNode "sql-policy" "Own instance, own credentials" "mcr.microsoft.com/mssql/server:2022-latest" {
                        containerInstance clp.policyDb
                    }
                    deploymentNode "sql-carrier" "Own instance, own credentials" "mcr.microsoft.com/mssql/server:2022-latest" {
                        containerInstance clp.carrierDb
                    }
                    deploymentNode "sql-ledger" "Own instance, own credentials" "mcr.microsoft.com/mssql/server:2022-latest" {
                        containerInstance clp.ledgerDb
                    }
                    deploymentNode "kafka" "Single node, KRaft" "apache/kafka:3.9.0" {
                        containerInstance clp.kafka
                    }
                    deploymentNode "kafka-ui" "Topic and consumer group browser" "kafbat/kafka-ui" "Tooling"
                    deploymentNode "structurizr" "Serves this model" "structurizr/lite" "Tooling"
                }
            }
        }
    }
    views {
        systemContext clp "SystemContext" "Who uses the platform and what it connects to." {
            include *
            autolayout lr
        }

        container clp "Containers" "Services, their databases and the event backbone." {
            include *
            autolayout lr
        }

        component clp.policy "PolicyComponents" "Inside the Policy Service." {
            include *
            autolayout lr
        }

        component clp.ledger "LedgerComponents" "Inside the Ledger Service." {
            include *
            autolayout lr
        }

        dynamic clp "QuoteBindBook" "Treaty publication, quote, bind and booking for the reference scenario." {
            treatyAdmin -> clp.carrier "Publishes treaty version HBR-CP-QS-2026.07"
            clp.carrier -> clp.carrierDb "Commits treaty version and outbox row"
            clp.carrier -> clp.kafka "Relay publishes treaty-version-published"
            clp.ledger -> clp.kafka "Consumes treaty-version-published"
            clp.ledger -> clp.ledgerDb "Stores treaty read model row and inbox row"
            broker -> clp.policy "POST /v1/quotes"
            clp.policy -> clp.rules "POST /v1/decisions: StraightThrough"
            clp.policy -> clp.rating "POST /v1/rates: 1410.75"
            clp.policy -> clp.policyDb "Stores revision, Decision, Rate Snapshot, outbox row"
            broker -> clp.policy "POST /v1/quotes/{id}/bind"
            clp.policy -> clp.rules "Re-decide"
            clp.policy -> clp.rating "Re-rate"
            clp.policy -> clp.policyDb "Bound revision, idempotency record, policy-bound outbox row: one transaction"
            clp.policy -> clp.kafka "Relay publishes policy-bound"
            clp.ledger -> clp.kafka "Consumes policy-bound"
            clp.ledger -> clp.ledgerDb "Appends transaction and lines, inbox row, premium-booked outbox row: one transaction"
            clp.ledger -> clp.kafka "Relay publishes premium-booked"
            clp.policy -> clp.kafka "Consumes premium-booked"
            clp.policy -> clp.policyDb "Booking Status Booked, inbox row"
            financeAnalyst -> clp.ledger "GET /v1/policies/{id}/transactions"
            autolayout lr
        }

        deployment clp local "LocalDeployment" "Aspire AppHost on a developer workstation." {
            include *
            autolayout lr
        }

        styles {
            element "Person" {
                shape person
            }
            element "Database" {
                shape cylinder
            }
            element "EventLog" {
                shape pipe
            }
            element "External" {
                background #8a8a8a
                color #ffffff
            }
            element "Planned" {
                border dashed
                opacity 70
            }
            element "Tooling" {
                border dotted
            }
            relationship "Planned" {
                style dashed
                opacity 60
            }
        }

        theme default
    }
}
