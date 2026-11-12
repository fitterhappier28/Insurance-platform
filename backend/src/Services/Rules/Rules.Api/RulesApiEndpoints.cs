using System.Globalization;
using Microsoft.AspNetCore.Http;
using Platform.Tenancy;
using Rules.Application;
using Rules.Domain;

namespace Rules.Api;

public static class RulesApiEndpoints
{
    public static void MapRulesApi(this WebApplication app)
    {
        app.MapPost("/v1/decisions", Decide);
        app.MapGet("/v1/rulesets", () => Results.Ok(new
        {
            items = new[] { new { version = RulesEvaluator.Version, effectiveFrom = RulesEvaluator.EffectiveFrom } }
        }));
    }

    private static IResult Decide(DecisionRequest? request, DecisionService service)
    {
        if (request?.Exposures is not { Count: > 0 } exposures || request.EffectiveDate is null)
        {
            return Problem("request-invalid", "Effective date and at least one Exposure are required.");
        }

        var mapped = new List<RiskExposure>(exposures.Count);
        foreach (var item in exposures)
        {
            if (!TryMap(item, request.EffectiveDate.Value, out var exposure))
            {
                return Problem("request-invalid", "Exposure fields are invalid.");
            }
            mapped.Add(exposure);
        }

        try
        {
            var result = service.Decide(request.EffectiveDate.Value, mapped, request.RulesetVersion);
            return Results.Ok(new
            {
                outcome = result.Outcome.ToString(),
                rulesetVersion = result.RulesetVersion,
                reasons = result.Reasons,
                exposures = result.Exposures.Select(exposure => new
                {
                    exposure.ExposureRef,
                    outcome = exposure.Outcome.ToString()
                })
            });
        }
        catch (UnsupportedRulesetException exception)
        {
            return Problem("ruleset-unavailable", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem("request-invalid", exception.Message);
        }
    }

    private static bool TryMap(ExposureRequest? request, DateOnly effectiveDate, out RiskExposure exposure)
    {
        exposure = default!;
        if (request is null
            || string.IsNullOrWhiteSpace(request.ExposureRef)
            || request.ExposureRef.Length > 64
            || request.CoverageCode != "BLDG"
            || !decimal.TryParse(request.Limit, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var limit)
            || request.NaicsCode is not { Length: 6 } naicsCode
            || !naicsCode.All(char.IsAsciiDigit)
            || request.Construction is not ("Frame" or "JoistedMasonry" or "NonCombustible" or "MasonryNonCombustible" or "FireResistive")
            || request.YearBuilt is null or < 1800 || request.YearBuilt > effectiveDate.Year
            || request.ProtectionClass is null or < 1 or > 10)
        {
            return false;
        }

        exposure = new RiskExposure(request.ExposureRef, request.CoverageCode, limit, naicsCode,
            request.Construction!, request.YearBuilt.Value, request.ProtectionClass.Value);
        return true;
    }

    private static IResult Problem(string code, string message) => new ProblemResult(code, message);

    private sealed class ProblemResult(string code, string message) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            await ProblemWriter.WriteAsync(httpContext, StatusCodes.Status422UnprocessableEntity, code, message);
        }
    }

    private sealed record DecisionRequest(DateOnly? EffectiveDate, string? RulesetVersion, List<ExposureRequest>? Exposures);
    private sealed record ExposureRequest(string? ExposureRef, string? CoverageCode, string? Limit, string? NaicsCode,
        string? Construction, int? YearBuilt, int? ProtectionClass);
}
