using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Rating.Tests;

public sealed class FR05_RatingApiTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient _client;

    public FR05_RatingApiTests(WebApplicationFactory<global::Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task FR05_rate_api_returns_string_money_and_factor_steps()
    {
        using var response = await PostRate("531110", 5);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("rateManualVersion").GetString().ShouldBe("CP-RATE-2026.07");
        root.GetProperty("totalPremium").GetString().ShouldBe("1410.75");
        root.GetProperty("currency").GetString().ShouldBe("USD");
        var exposure = root.GetProperty("exposures")[0];
        exposure.GetProperty("baseRate").GetString().ShouldBe("0.2500");
        exposure.GetProperty("premium").GetString().ShouldBe("1410.75");
        exposure.GetProperty("factors")[0].GetProperty("premiumAfter").GetString().ShouldBe("1250.00");
        exposure.GetProperty("factors")[3].GetProperty("premiumAfter").GetString().ShouldBe("1410.75");
    }

    [Theory]
    [InlineData("541110", 5)]
    [InlineData("531110", 10)]
    public async Task FR05_rate_api_returns_problem_for_unrateable_exposure(string naicsCode, int protectionClass)
    {
        using var response = await PostRate(naicsCode, protectionClass);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().ShouldBe("not-rateable");
        document.RootElement.GetProperty("status").GetInt32().ShouldBe(422);
    }

    [Fact]
    public async Task FR16_rating_api_serves_the_authoritative_openapi_contract()
    {
        var served = await _client.GetStringAsync("/openapi/v1.yaml");
        var contractPath = Path.Combine(RepoRoot(), "specs", "contracts", "openapi", "rating.v1.yaml");
        served.Replace("\r\n", "\n", StringComparison.Ordinal)
            .ShouldBe(File.ReadAllText(contractPath).Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    private async Task<HttpResponseMessage> PostRate(string naicsCode, int protectionClass)
    {
        var body = $$"""
            {"effectiveDate":"2026-07-01","rateManualVersion":"CP-RATE-2026.07",
             "exposures":[{"exposureRef":"exp-1","coverageCode":"BLDG","limit":"500000","naicsCode":"{{naicsCode}}","construction":"JoistedMasonry","yearBuilt":1998,"protectionClass":{{protectionClass}}}]}
            """;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/rates")
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
