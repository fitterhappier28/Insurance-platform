using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Rules.Tests;

public sealed class FR04_RulesApiTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient _client;

    public FR04_RulesApiTests(WebApplicationFactory<global::Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task FR04_decision_api_returns_contract_fixture_for_reference_exposure()
    {
        using var response = await PostDecision("531110", "CP-RULES-2026.07");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("outcome").GetString().ShouldBe("StraightThrough");
        root.GetProperty("rulesetVersion").GetString().ShouldBe("CP-RULES-2026.07");
        root.GetProperty("reasons").GetArrayLength().ShouldBe(0);
        root.GetProperty("exposures")[0].GetProperty("exposureRef").GetString().ShouldBe("exp-1");
    }

    [Fact]
    public async Task FR04_decision_api_declines_unsupported_naics()
    {
        using var response = await PostDecision("541110", null);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("outcome").GetString().ShouldBe("Decline");
        document.RootElement.GetProperty("reasons")[0].GetProperty("code").GetString()
            .ShouldBe("D-NAICS-UNSUPPORTED");
    }

    [Fact]
    public async Task FR04_decision_api_returns_refer_for_old_building()
    {
        using var response = await PostDecision("531110", null, yearBuilt: 1972);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("outcome").GetString().ShouldBe("Refer");
        document.RootElement.GetProperty("reasons")[0].GetProperty("code").GetString()
            .ShouldBe("R-BUILDING-AGE");
    }

    [Fact]
    public async Task FR04_decision_api_declines_protection_class_ten()
    {
        using var response = await PostDecision("531110", null, protectionClass: 10);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("outcome").GetString().ShouldBe("Decline");
        document.RootElement.GetProperty("reasons")[0].GetProperty("code").GetString()
            .ShouldBe("D-PROTECTION-CLASS");
    }

    [Fact]
    public async Task FR04_decision_api_rejects_unpublished_ruleset_as_problem()
    {
        using var response = await PostDecision("531110", "CP-RULES-2025.01");
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().ShouldBe("ruleset-unavailable");
        document.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FR13_decision_api_requires_tenant_header()
    {
        using var response = await _client.PostAsync("/v1/decisions", new StringContent("{}", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().ShouldBe("tenant-required");
    }

    [Fact]
    public async Task FR16_rules_api_serves_the_authoritative_openapi_contract()
    {
        var served = await _client.GetStringAsync("/openapi/v1.yaml");
        var contractPath = Path.Combine(RepoRoot(), "specs", "contracts", "openapi", "rules.v1.yaml");
        served.Replace("\r\n", "\n", StringComparison.Ordinal)
            .ShouldBe(File.ReadAllText(contractPath).Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    private async Task<HttpResponseMessage> PostDecision(string naicsCode, string? version, int yearBuilt = 1998, int protectionClass = 5)
    {
        var versionProperty = version is null ? "," : $",\"rulesetVersion\":\"{version}\",";
        var body = $$"""
            {"effectiveDate":"2026-07-01"{{versionProperty}}
             "exposures":[{"exposureRef":"exp-1","coverageCode":"BLDG","limit":"500000","naicsCode":"{{naicsCode}}","construction":"JoistedMasonry","yearBuilt":{{yearBuilt}},"protectionClass":{{protectionClass}}}]}
            """;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Tenant-Id", "harbor");
        return await _client.SendAsync(request);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "specs")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
