using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Policy.Application;
using Policy.Domain;

namespace Policy.Api;

internal sealed class PolicyDependencyException(string code, string message, bool unavailable) : Exception(message)
{
    public string Code { get; } = code;
    public bool Unavailable { get; } = unavailable;
}

internal static class PolicyQuoteEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<PolicyEvaluation> EvaluateAsync(
        Schedule schedule,
        DateOnly effectiveDate,
        string tenantId,
        string correlationId,
        string? rulesetVersion,
        string? rateManualVersion,
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        var exposures = schedule.Exposures.Select(exposure => new
        {
            exposureRef = exposure.Ref,
            coverageCode = exposure.CoverageCode,
            limit = Money(exposure.Limit),
            naicsCode = exposure.NaicsCode,
            construction = exposure.Construction,
            yearBuilt = exposure.YearBuilt,
            protectionClass = exposure.ProtectionClass
        }).ToArray();

        var decisionBody = new
        {
            effectiveDate = effectiveDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            rulesetVersion,
            exposures
        };
        using var decisionResponse = await SendAsync(clientFactory.CreateClient("rules"), "/v1/decisions",
            decisionBody, tenantId, correlationId, cancellationToken);
        var decisionJson = await decisionResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!decisionResponse.IsSuccessStatusCode)
        {
            var code = ReadProblemCode(decisionJson) ?? "rules-unavailable";
            throw new PolicyDependencyException(code, "Rules could not evaluate the Quote.",
                (int)decisionResponse.StatusCode >= 500);
        }

        using var decisionDocument = JsonDocument.Parse(decisionJson);
        var decision = decisionDocument.RootElement;
        var outcome = decision.GetProperty("outcome").GetString()
            ?? throw new PolicyDependencyException("rules-invalid-response", "Rules returned an invalid response.", true);
        if (outcome is not ("StraightThrough" or "Refer" or "Decline"))
        {
            throw new PolicyDependencyException("rules-invalid-response", "Rules returned an unsupported Decision outcome.", true);
        }
        var pinnedRuleset = decision.GetProperty("rulesetVersion").GetString()
            ?? throw new PolicyDependencyException("rules-invalid-response", "Rules returned no Ruleset version.", true);
        if (outcome == "Decline")
        {
            return new PolicyEvaluation(outcome, pinnedRuleset, decisionJson, null, null, null);
        }

        var rateBody = new
        {
            effectiveDate = effectiveDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            rateManualVersion,
            exposures
        };
        using var rateResponse = await SendAsync(clientFactory.CreateClient("rating"), "/v1/rates",
            rateBody, tenantId, correlationId, cancellationToken);
        var rateJson = await rateResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!rateResponse.IsSuccessStatusCode)
        {
            var code = ReadProblemCode(rateJson) ?? "rating-unavailable";
            throw new PolicyDependencyException(code, "Rating could not price the Quote.",
                (int)rateResponse.StatusCode >= 500);
        }

        using var rateDocument = JsonDocument.Parse(rateJson);
        var rate = rateDocument.RootElement;
        var pinnedRateManual = rate.GetProperty("rateManualVersion").GetString()
            ?? throw new PolicyDependencyException("rating-invalid-response", "Rating returned no Rate Manual version.", true);
        var writtenPremium = ParseMoney(rate.GetProperty("totalPremium").GetString());
        var exposureIds = schedule.Exposures.ToDictionary(exposure => exposure.Ref, exposure => exposure.ExposureId,
            StringComparer.Ordinal);
        var ratingSnapshot = new
        {
            rateManualVersion = pinnedRateManual,
            writtenPremium = Money(writtenPremium),
            currency = rate.GetProperty("currency").GetString(),
            exposures = rate.GetProperty("exposures").EnumerateArray().Select(rated =>
            {
                var reference = rated.GetProperty("exposureRef").GetString()!;
                if (!exposureIds.TryGetValue(reference, out var exposureId))
                {
                    throw new PolicyDependencyException("rating-invalid-response", "Rating returned an unknown Exposure.", true);
                }
                return new
                {
                    exposureId,
                    @ref = reference,
                    baseRate = rated.GetProperty("baseRate").GetString(),
                    premium = rated.GetProperty("premium").GetString(),
                    factors = rated.GetProperty("factors").EnumerateArray().Select(factor => new
                    {
                        step = factor.GetProperty("step").GetInt32(),
                        code = factor.GetProperty("code").GetString(),
                        value = factor.GetProperty("value").GetString(),
                        premiumAfter = factor.GetProperty("premiumAfter").GetString()
                    }).ToArray()
                };
            }).ToArray()
        };

        return new PolicyEvaluation(outcome, pinnedRuleset, decisionJson,
            JsonSerializer.Serialize(ratingSnapshot, JsonOptions), pinnedRateManual, writtenPremium);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string path,
        object body,
        string tenantId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body, options: JsonOptions)
            };
            request.Headers.Add("X-Tenant-Id", tenantId);
            request.Headers.Add("X-Correlation-Id", correlationId);
            try
            {
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode >= 500 && attempt == 0)
                {
                    response.Dispose();
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                    continue;
                }
                if ((int)response.StatusCode >= 500)
                {
                    response.Dispose();
                    throw new PolicyDependencyException("dependency-unavailable", "Rules or Rating is unavailable.", true);
                }
                return response;
            }
            catch (HttpRequestException) when (attempt == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new PolicyDependencyException("dependency-unavailable", exception.Message, true);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw new PolicyDependencyException("dependency-timeout", exception.Message, true);
            }
        }
        throw new PolicyDependencyException("dependency-unavailable", "Rules or Rating is unavailable.", true);
    }

    private static string? ReadProblemCode(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static decimal ParseMoney(string? value) => decimal.TryParse(value, NumberStyles.AllowDecimalPoint,
        CultureInfo.InvariantCulture, out var money)
        ? money
        : throw new PolicyDependencyException("rating-invalid-response", "Rating returned invalid money.", true);

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
