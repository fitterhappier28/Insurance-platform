using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Data.SqlClient;
using Platform.Tests;
using Shouldly;

namespace Acceptance.Tests;

public sealed class StackFixture : IAsyncLifetime
{
    public const string PolicySa = "PolicySa#2026x";
    public const string PolicyRuntime = "PolicyRt#2026x";
    public const string LedgerSa = "LedgerSa#2026x";

    public DistributedApplication App { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        Set(builder, "sql-policy-password", PolicySa);
        Set(builder, "sql-policy-owner-password", "PolicyOw#2026x");
        Set(builder, "sql-policy-runtime-password", PolicyRuntime);
        Set(builder, "sql-ledger-password", LedgerSa);
        Set(builder, "sql-ledger-owner-password", "LedgerOw#2026x");
        Set(builder, "sql-ledger-runtime-password", "LedgerRt#2026x");

        App = await builder.BuildAsync();
        await App.StartAsync();
        foreach (var name in new[] { "policy-api", "rules-api", "rating-api", "ledger-api" })
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8));
            await App.ResourceNotifications.WaitForResourceHealthyAsync(name, timeout.Token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
    }

    private static void Set(IDistributedApplicationTestingBuilder builder, string name, string value) =>
        builder.Configuration[$"Parameters:{name}"] = value;
}

[Trait("Category", "Acceptance")]
public class StackTests(StackFixture stack) : IClassFixture<StackFixture>
{
    [Fact]
    public async Task NFR09_health_ready_returns_200()
    {
        foreach (var name in new[] { "policy-api", "rules-api", "rating-api", "ledger-api" })
        {
            var client = stack.App.CreateHttpClient(name);
            var response = await client.GetAsync("/health/ready");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var served = await client.GetStringAsync("/openapi/v1.yaml");
            var contract = name.Split('-')[0] + ".v1.yaml";
            OpenApiContract.AssertServedDocument(served, Path.Combine(OpenApiContract.RepoRoot(), "specs", "contracts", "openapi", contract));
        }
    }

    [Fact]
    public async Task FR13_missing_tenant_returns_tenant_required()
    {
        var probes = new (string Service, string Path, string Contract, string Operation)[]
        {
            ("policy-api", "/v1/accounts", "policy.v1.yaml", "createAccount"),
            ("rules-api", "/v1/decisions", "rules.v1.yaml", "decide"),
            ("rating-api", "/v1/rates", "rating.v1.yaml", "rate"),
        };

        foreach (var probe in probes)
        {
            var client = stack.App.CreateHttpClient(probe.Service);
            var response = await client.PostAsync(probe.Path, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("tenant-required");
            OpenApiContract.AssertOperationResponse(
                Path.Combine(OpenApiContract.RepoRoot(), "specs", "contracts", "openapi", probe.Contract),
                probe.Operation,
                "400",
                body);
        }

        var ledger = stack.App.CreateHttpClient("ledger-api");
        var ledgerResponse = await ledger.GetAsync("/v1/reconciliation/self-check");
        ledgerResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var ledgerBody = await ledgerResponse.Content.ReadAsStringAsync();
        OpenApiContract.AssertProblemSchema(ledgerBody);
    }

    [Fact]
    public async Task NFR17_login_from_one_instance_is_rejected_by_another()
    {
        var rejected = new SqlConnectionStringBuilder
        {
            DataSource = "127.0.0.1,14332",
            UserID = "sa",
            Password = StackFixture.PolicySa,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        };
        await using var connection = new SqlConnection(rejected.ConnectionString);
        await Should.ThrowAsync<SqlException>(() => connection.OpenAsync());
    }

    [Fact]
    public async Task NFR29_runtime_login_reads_its_own_migration_history()
    {
        var own = new SqlConnectionStringBuilder
        {
            DataSource = "127.0.0.1,14331",
            InitialCatalog = "PolicyDb",
            UserID = "policy_runtime",
            Password = StackFixture.PolicyRuntime,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        };
        await using var connection = new SqlConnection(own.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory";
        var count = (int)(await command.ExecuteScalarAsync())!;
        count.ShouldBeGreaterThan(0);
    }
}
