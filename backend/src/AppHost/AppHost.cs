var app = ClpAppHost.Create(args);
app.Run();

public static class ClpAppHost
{
    public static DistributedApplication Create(string[] args) => CreateBuilder(args).Build();

    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        AddResources(builder);
        return builder;
    }

    public static void AddResources(IDistributedApplicationBuilder builder)
    {
        // Secret parameters. On first run Aspire writes generated values to this AppHost's user secrets.
        // sql-*-password: sa password for the instance.
        // sql-*-owner-password: migration login. sql-*-runtime-password: request login.
        var policySa = builder.AddParameter("sql-policy-password", secret: true);
        var policyOwner = builder.AddParameter("sql-policy-owner-password", secret: true);
        var policyRuntime = builder.AddParameter("sql-policy-runtime-password", secret: true);
        var ledgerSa = builder.AddParameter("sql-ledger-password", secret: true);
        var ledgerOwner = builder.AddParameter("sql-ledger-owner-password", secret: true);
        var ledgerRuntime = builder.AddParameter("sql-ledger-runtime-password", secret: true);

        var sqlPolicy = builder.AddSqlServer("sql-policy", policySa, port: 14331)
            .WithDataVolume("clp-sql-policy")
            .WithContainerRuntimeArgs("--memory=2g");
        var policyDb = sqlPolicy.AddDatabase("policy-db", "PolicyDb");

        var sqlLedger = builder.AddSqlServer("sql-ledger", ledgerSa, port: 14332)
            .WithDataVolume("clp-sql-ledger")
            .WithContainerRuntimeArgs("--memory=2g");
        var ledgerDb = sqlLedger.AddDatabase("ledger-db", "LedgerDb");

        var loginsScript = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "db", "logins.sql"));
        var policyLogins = AddLoginInit(builder, "sql-policy-logins", "sql-policy", "PolicyDb", "policy_owner", "policy_runtime", policySa, policyOwner, policyRuntime, loginsScript, sqlPolicy);
        var ledgerLogins = AddLoginInit(builder, "sql-ledger-logins", "sql-ledger", "LedgerDb", "ledger_owner", "ledger_runtime", ledgerSa, ledgerOwner, ledgerRuntime, loginsScript, sqlLedger);

        var kafka = builder.AddKafka("kafka", port: 9092);
        kafka.WithKafkaUI(ui => ui.WithHostPort(8085), "kafka-ui");

        var topics = builder.AddContainer("kafka-topics", "confluentinc/confluent-local", "8.2.0")
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", TopicScript)
            .WaitFor(kafka);

        var architecture = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "..", "docs", "architecture"));
        builder.AddContainer("structurizr", "structurizr/lite")
            .WithHttpEndpoint(port: 8086, targetPort: 8080)
            .WithBindMount(architecture, "/usr/local/structurizr");

        var rules = builder.AddProject<Projects.Rules_Api>("rules-api")
            .WithHttpEndpoint(port: 5102, name: "http");
        var rating = builder.AddProject<Projects.Rating_Api>("rating-api")
            .WithHttpEndpoint(port: 5103, name: "http");

        builder.AddProject<Projects.Policy_Api>("policy-api")
            .WithHttpEndpoint(port: 5101, name: "http")
            .WithReference(policyDb)
            .WithReference(kafka)
            .WithEnvironment("ConnectionStrings__policy-owner", LoginConnection("127.0.0.1,14331", "PolicyDb", "policy_owner", policyOwner))
            .WithEnvironment("ConnectionStrings__policy-runtime", LoginConnection("127.0.0.1,14331", "PolicyDb", "policy_runtime", policyRuntime))
            .WaitFor(policyDb)
            .WaitForCompletion(policyLogins)
            .WaitFor(kafka)
            .WaitForCompletion(topics);

        builder.AddProject<Projects.Ledger_Api>("ledger-api")
            .WithHttpEndpoint(port: 5104, name: "http")
            .WithReference(ledgerDb)
            .WithReference(kafka)
            .WithEnvironment("ConnectionStrings__ledger-owner", LoginConnection("127.0.0.1,14332", "LedgerDb", "ledger_owner", ledgerOwner))
            .WithEnvironment("ConnectionStrings__ledger-runtime", LoginConnection("127.0.0.1,14332", "LedgerDb", "ledger_runtime", ledgerRuntime))
            .WaitFor(ledgerDb)
            .WaitForCompletion(ledgerLogins)
            .WaitFor(kafka)
            .WaitForCompletion(topics);

        _ = rules;
        _ = rating;
    }

    private static IResourceBuilder<ContainerResource> AddLoginInit(
        IDistributedApplicationBuilder builder,
        string name,
        string serverName,
        string databaseName,
        string ownerLogin,
        string runtimeLogin,
        IResourceBuilder<ParameterResource> saPassword,
        IResourceBuilder<ParameterResource> ownerPassword,
        IResourceBuilder<ParameterResource> runtimePassword,
        string scriptPath,
        IResourceBuilder<SqlServerServerResource> server)
    {
        return builder.AddContainer(name, "mcr.microsoft.com/mssql/server", "2022-latest")
            .WithEntrypoint("/bin/bash")
            .WithBindMount(scriptPath, "/init/logins.sql", isReadOnly: true)
            .WithEnvironment("SA_PASSWORD", saPassword)
            .WithEnvironment("OWNER_PASSWORD", ownerPassword)
            .WithEnvironment("RUNTIME_PASSWORD", runtimePassword)
            .WithArgs("-c", Lf($$"""
                set -e
                for i in $(seq 1 30); do
                  /opt/mssql-tools18/bin/sqlcmd -S {{serverName}} -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" && break
                  sleep 2
                done
                /opt/mssql-tools18/bin/sqlcmd -S {{serverName}} -U sa -P "$SA_PASSWORD" -C -b -i /init/logins.sql -v DatabaseName={{databaseName}} OwnerLogin={{ownerLogin}} RuntimeLogin={{runtimeLogin}} OwnerPassword="$OWNER_PASSWORD" RuntimePassword="$RUNTIME_PASSWORD"
                """))
            .WaitFor(server);
    }

    private static ReferenceExpression LoginConnection(string dataSource, string database, string login, IResourceBuilder<ParameterResource> password) =>
        ReferenceExpression.Create($"Server={dataSource};Database={database};User ID={login};Password={password};TrustServerCertificate=true");

    // Scripts run in Linux containers; source files may be checked out with CRLF on Windows.
    private static string Lf(string script) => script.Replace("\r\n", "\n").Replace("\r", "\n");

    private static readonly string TopicScript = Lf("""
        set -e
        ready=0
        for i in 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30; do
          if kafka-topics --bootstrap-server kafka:9092 --list >/dev/null 2>&1; then
            ready=1
            break
          fi
          sleep 2
        done
        if [ "$ready" != "1" ]; then
          echo "kafka did not accept topic commands" >&2
          exit 1
        fi
        kafka-topics --bootstrap-server kafka:9092 --create --if-not-exists --topic clp.policy.v1 --partitions 6 --replication-factor 1 --config retention.ms=1209600000
        kafka-topics --bootstrap-server kafka:9092 --create --if-not-exists --topic clp.ledger.v1 --partitions 6 --replication-factor 1 --config retention.ms=1209600000
        kafka-topics --bootstrap-server kafka:9092 --create --if-not-exists --topic clp.policy.v1.ledger.dlt --partitions 1 --replication-factor 1 --config retention.ms=-1
        kafka-topics --bootstrap-server kafka:9092 --create --if-not-exists --topic clp.ledger.v1.policy.dlt --partitions 1 --replication-factor 1 --config retention.ms=-1
        """);
}
