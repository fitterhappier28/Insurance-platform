using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Data.SqlClient;
using Platform.Tests;
using Shouldly;
using System.Text;

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

    [Fact]
    public async Task FR02_account_create_is_idempotent_and_tenant_scoped()
    {
        var client = stack.App.CreateHttpClient("policy-api");
        const string accountBody = """
            {"name":"Bayview Properties LLC","applicant":{"legalName":"Bayview Properties LLC","fein":"12-3456789","mailingAddress":{"street":"100 Bay St","city":"Tampa","state":"FL","postalCode":"33602"}}}
            """;
        using var first = await Send(client, HttpMethod.Post, "/v1/accounts", accountBody, "account-fr02");
        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        var firstText = await first.Content.ReadAsStringAsync();
        using var replay = await Send(client, HttpMethod.Post, "/v1/accounts", accountBody, "account-fr02");
        replay.StatusCode.ShouldBe(HttpStatusCode.Created);
        replay.Headers.GetValues("Idempotent-Replayed").Single().ShouldBe("true");
        (await replay.Content.ReadAsStringAsync()).ShouldBe(firstText);

        using var differentRequest = await Send(client, HttpMethod.Post, "/v1/accounts",
            accountBody.Replace("Bayview Properties LLC", "Different Account", StringComparison.Ordinal), "account-fr02");
        differentRequest.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        using var accountJson = JsonDocument.Parse(firstText);
        var accountId = accountJson.RootElement.GetProperty("accountId").GetGuid();
        using var crossTenantRead = await Send(client, HttpMethod.Get, $"/v1/accounts/{accountId}", null,
            tenantId: "northstar");
        crossTenantRead.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FR06_quote_produces_reference_snapshot_and_FR07_bind_is_idempotent_with_atomic_outbox()
    {
        var client = stack.App.CreateHttpClient("policy-api");
        var accountId = await CreateAccount(client);
        var quoteBody = QuoteBody(accountId, 1998, 5);
        using var quoteResponse = await Send(client, HttpMethod.Post, "/v1/quotes", quoteBody, "reference-quote-bind");
        quoteResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var quoteJson = JsonDocument.Parse(await quoteResponse.Content.ReadAsStringAsync());
        var quote = quoteJson.RootElement;
        quote.GetProperty("status").GetString().ShouldBe("Quoted");
        quote.GetProperty("decision").GetProperty("rulesetVersion").GetString().ShouldBe("CP-RULES-2026.07");
        quote.GetProperty("rating").GetProperty("writtenPremium").GetString().ShouldBe("1410.75");
        var quoteId = quote.GetProperty("quoteId").GetGuid();
        var policyId = quote.GetProperty("policyId").GetGuid();

        using var bind = await Send(client, HttpMethod.Post, $"/v1/quotes/{quoteId}/bind", "{}", "reference-bind");
        bind.StatusCode.ShouldBe(HttpStatusCode.OK);
        var bindBody = await bind.Content.ReadAsStringAsync();
        using var bindJson = JsonDocument.Parse(bindBody);
        bindJson.RootElement.GetProperty("writtenPremium").GetString().ShouldBe("1410.75");
        bindJson.RootElement.GetProperty("bookingStatus").GetString().ShouldBe("Pending");
        using var replay = await Send(client, HttpMethod.Post, $"/v1/quotes/{quoteId}/bind", "{}", "reference-bind");
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.Headers.GetValues("Idempotent-Replayed").Single().ShouldBe("true");
        (await replay.Content.ReadAsStringAsync()).ShouldBe(bindBody);

        using var policyResponse = await Send(client, HttpMethod.Get, $"/v1/policies/{policyId}", null);
        policyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var policyJson = JsonDocument.Parse(await policyResponse.Content.ReadAsStringAsync());
        policyJson.RootElement.GetProperty("bookingStatus").GetString().ShouldBe("Pending");

        await using var connection = await OpenPolicyRuntimeConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.Status, COUNT(o.MessageId)
            FROM PolicyRevisions r
            LEFT JOIN PolicyOutbox o ON JSON_VALUE(o.PayloadJson, '$.data.policyId') = CONVERT(nvarchar(36), r.PolicyId)
              AND o.Type = 'clp.policy.policy-bound.v1'
            WHERE r.QuoteId = @QuoteId
            GROUP BY r.Status
            """;
        command.Parameters.AddWithValue("@QuoteId", quoteId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        reader.GetString(0).ShouldBe("Bound");
        reader.GetInt32(1).ShouldBe(1);
        await reader.DisposeAsync();

        await using var immutabilityCheck = connection.CreateCommand();
        immutabilityCheck.CommandText = "UPDATE PolicyRevisions SET ScheduleJson = N'{}' WHERE QuoteId = @QuoteId";
        immutabilityCheck.Parameters.AddWithValue("@QuoteId", quoteId);
        await Should.ThrowAsync<SqlException>(() => immutabilityCheck.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task FR06_quote_keeps_price_for_refer_and_omits_price_for_decline()
    {
        var client = stack.App.CreateHttpClient("policy-api");
        var accountId = await CreateAccount(client);
        using var refer = await Send(client, HttpMethod.Post, "/v1/quotes", QuoteBody(accountId, 1972, 5), "refer-quote");
        refer.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var referJson = JsonDocument.Parse(await refer.Content.ReadAsStringAsync());
        referJson.RootElement.GetProperty("status").GetString().ShouldBe("Referred");
        referJson.RootElement.GetProperty("rating").GetProperty("writtenPremium").ValueKind.ShouldBe(JsonValueKind.String);

        using var decline = await Send(client, HttpMethod.Post, "/v1/quotes", QuoteBody(accountId, 1998, 10), "decline-quote");
        decline.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var declineJson = JsonDocument.Parse(await decline.Content.ReadAsStringAsync());
        declineJson.RootElement.GetProperty("status").GetString().ShouldBe("Declined");
        declineJson.RootElement.GetProperty("rating").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task FR07_expired_quote_cannot_bind()
    {
        var client = stack.App.CreateHttpClient("policy-api");
        var accountId = await CreateAccount(client);
        using var response = await Send(client, HttpMethod.Post, "/v1/quotes", QuoteBody(accountId, 1998, 5), "expired-quote");
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var quoteId = document.RootElement.GetProperty("quoteId").GetGuid();
        await using var connection = await OpenPolicyRuntimeConnection();
        await using (var update = connection.CreateCommand())
        {
            update.CommandText = "UPDATE PolicyRevisions SET ExpiresAt = DATEADD(day, -1, SYSUTCDATETIME()) WHERE QuoteId = @QuoteId";
            update.Parameters.AddWithValue("@QuoteId", quoteId);
            (await update.ExecuteNonQueryAsync()).ShouldBe(1);
        }
        using var bind = await Send(client, HttpMethod.Post, $"/v1/quotes/{quoteId}/bind", "{}", "expired-bind");
        bind.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await bind.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("quote-expired");
    }

    private static async Task<Guid> CreateAccount(HttpClient client)
    {
        const string body = """
            {"name":"Bayview Properties LLC","applicant":{"legalName":"Bayview Properties LLC","fein":"12-3456789","mailingAddress":{"street":"100 Bay St","city":"Tampa","state":"FL","postalCode":"33602"}}}
            """;
        using var response = await Send(client, HttpMethod.Post, "/v1/accounts", body, "account-fr02");
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accountId").GetGuid();
    }

    private static string QuoteBody(Guid accountId, int yearBuilt, int protectionClass) => $$"""
        {"accountId":"{{accountId:D}}","effectiveFrom":"2026-12-01","effectiveTo":"2027-12-01",
         "coverages":[{"code":"BLDG","deductible":"5000"}],
         "locations":[{"ref":"loc-1","address":{"street":"100 Bay St","city":"Tampa","state":"FL","postalCode":"33602"},"protectionClass":{{protectionClass}},"buildings":[{"ref":"bldg-1","construction":"JoistedMasonry","yearBuilt":{{yearBuilt}},"squareFeet":10000}]}],
         "exposures":[{"ref":"exp-1","coverageCode":"BLDG","subjectRef":"bldg-1","limit":"500000","naicsCode":"531110"}]}
        """;

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path,
        string? body, string? idempotencyKey = null, string tenantId = "harbor")
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Tenant-Id", tenantId);
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString("D"));
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }
        return await client.SendAsync(request);
    }

    private static async Task<SqlConnection> OpenPolicyRuntimeConnection()
    {
        var connection = new SqlConnection(new SqlConnectionStringBuilder
        {
            DataSource = "127.0.0.1,14331",
            InitialCatalog = "PolicyDb",
            UserID = "policy_runtime",
            Password = StackFixture.PolicyRuntime,
            TrustServerCertificate = true,
            ConnectTimeout = 10
        }.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
