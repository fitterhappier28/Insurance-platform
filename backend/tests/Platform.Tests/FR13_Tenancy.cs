using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Platform.Tests;

public class FR13_Tenancy : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public FR13_Tenancy(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task FR13_missing_tenant_returns_tenant_required()
    {
        var response = await factory.CreateClient().PostAsync("/v1/accounts", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("tenant-required");
        var contract = Path.Combine(OpenApiContract.RepoRoot(), "specs", "contracts", "openapi", "policy.v1.yaml");
        OpenApiContract.AssertOperationResponse(contract, "createAccount", "400", body);
    }

    [Fact]
    public async Task FR13_unknown_tenant_returns_tenant_unknown()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "other");
        var response = await client.GetAsync("/v1/accounts/00000000-0000-0000-0000-000000000001");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("tenant-unknown");
    }

    [Fact]
    public async Task NFR09_health_live_and_ready_do_not_require_a_tenant()
    {
        var client = factory.CreateClient();
        (await client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/swagger/index.html")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var served = await client.GetStringAsync("/openapi/v1.yaml");
        OpenApiContract.AssertServedDocument(served, Path.Combine(OpenApiContract.RepoRoot(), "specs", "contracts", "openapi", "policy.v1.yaml"));
    }
}
