using System.Globalization;
using Microsoft.AspNetCore.Http;
using Platform.Tenancy;
using Rating.Application;
using Rating.Domain;

namespace Rating.Api;

public static class RatingApiEndpoints
{
    public static void MapRatingApi(this WebApplication app)
    {
        app.MapPost("/v1/rates", Rate);
        app.MapGet("/v1/rate-manuals", () => Results.Ok(new
        {
            items = new[] { new { version = RateManual.Version, effectiveFrom = RateManual.EffectiveFrom } }
        }));
    }

    private static IResult Rate(RateRequest? request, RatingService service)
    {
        if (request?.Exposures is not { Count: > 0 } exposures || request.EffectiveDate is null)
        {
            return Problem("request-invalid", "Effective date and at least one Exposure are required.");
        }

        var mapped = new List<RiskExposure>(exposures.Count);
        foreach (var item in exposures)
        {
            if (!TryMap(item, out var exposure))
            {
                return Problem("request-invalid", "Exposure fields are invalid.");
            }
            mapped.Add(exposure);
        }

        try
        {
            var result = service.Rate(request.EffectiveDate.Value, mapped, request.RateManualVersion);
            return Results.Ok(new
            {
                rateManualVersion = result.RateManualVersion,
                totalPremium = Money(result.TotalPremium),
                currency = result.Currency,
                exposures = result.Exposures.Select(exposure => new
                {
                    exposure.ExposureRef,
                    baseRate = RateString(exposure.BaseRate),
                    premium = Money(exposure.Premium),
                    factors = exposure.Factors.Select(factor => new
                    {
                        factor.Step,
                        factor.Code,
                        value = RateString(factor.Value),
                        premiumAfter = Money(factor.PremiumAfter)
                    })
                })
            });
        }
        catch (NotRateableException exception)
        {
            return Problem("not-rateable", exception.Message);
        }
        catch (UnsupportedRateManualException exception)
        {
            return Problem("rate-manual-unavailable", exception.Message);
        }
    }

    private static bool TryMap(ExposureRequest? request, out RiskExposure exposure)
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
            || request.YearBuilt is null or < 1800 or > 2100
            || request.ProtectionClass is null or < 1 or > 10)
        {
            return false;
        }

        exposure = new RiskExposure(request.ExposureRef, request.CoverageCode, limit, naicsCode,
            request.Construction!, request.YearBuilt.Value, request.ProtectionClass.Value);
        return true;
    }

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string RateString(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static IResult Problem(string code, string detail) => new ProblemResult(code, detail);

    private sealed class ProblemResult(string code, string detail) : IResult
    {
        public async Task ExecuteAsync(HttpContext context)
        {
            await ProblemWriter.WriteAsync(context, StatusCodes.Status422UnprocessableEntity, code, detail);
        }
    }

    private sealed record RateRequest(DateOnly? EffectiveDate, string? RateManualVersion, List<ExposureRequest>? Exposures);
    private sealed record ExposureRequest(string? ExposureRef, string? CoverageCode, string? Limit, string? NaicsCode,
        string? Construction, int? YearBuilt, int? ProtectionClass);
}
