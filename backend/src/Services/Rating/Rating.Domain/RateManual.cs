namespace Rating.Domain;

public static class RateManual
{
    public const string Version = "CP-RATE-2026.07";
    public static readonly DateOnly EffectiveFrom = new(2026, 7, 1);

    private static readonly IReadOnlyDictionary<string, decimal> BaseRates = new Dictionary<string, decimal>(StringComparer.Ordinal)
    {
        ["Frame"] = 0.4000m,
        ["JoistedMasonry"] = 0.2500m,
        ["NonCombustible"] = 0.2200m,
        ["MasonryNonCombustible"] = 0.2000m,
        ["FireResistive"] = 0.1500m
    };

    public static RateSnapshot Calculate(
        DateOnly effectiveDate,
        IReadOnlyList<RiskExposure> exposures,
        string? rateManualVersion = null)
    {
        if (exposures.Count == 0)
        {
            throw new ArgumentException("At least one Exposure is required.", nameof(exposures));
        }

        if (rateManualVersion is not null && !string.Equals(rateManualVersion, Version, StringComparison.Ordinal))
        {
            throw new UnsupportedRateManualException(rateManualVersion);
        }

        if (effectiveDate < EffectiveFrom)
        {
            throw new UnsupportedRateManualException($"effective date {effectiveDate:yyyy-MM-dd}");
        }

        var ratedExposures = exposures.Select(exposure => Rate(effectiveDate, exposure)).ToArray();
        return new RateSnapshot(Version, ratedExposures.Sum(exposure => exposure.Premium), "USD", ratedExposures);
    }

    public static decimal RoundPremium(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static RatedExposure Rate(DateOnly effectiveDate, RiskExposure exposure)
    {
        if (exposure.CoverageCode != "BLDG")
        {
            throw new NotRateableException($"Coverage '{exposure.CoverageCode}' is not rateable.");
        }
        if (exposure.Limit is < 1m or > 25_000_000m)
        {
            throw new NotRateableException("Exposure limit must be between 1 and 25,000,000.");
        }
        if (exposure.NaicsCode is not ("531110" or "722511"))
        {
            throw new NotRateableException($"NAICS code '{exposure.NaicsCode}' is not rateable.");
        }
        if (exposure.ProtectionClass is < 1 or > 9)
        {
            throw new NotRateableException("Protection class must be between 1 and 9 for rating.");
        }
        if (exposure.YearBuilt is < 1800 || exposure.YearBuilt > effectiveDate.Year)
        {
            throw new NotRateableException("Year built must be between 1800 and the effective year.");
        }
        if (!BaseRates.TryGetValue(exposure.Construction, out var baseRate))
        {
            throw new NotRateableException($"Construction '{exposure.Construction}' is not rateable.");
        }

        var basePremium = exposure.Limit / 100m * baseRate;
        var factors = new List<RatedFactor>(4)
        {
            new(0, "BASE_RATE", baseRate, RoundPremium(basePremium))
        };
        var cumulativeFactor = 1m;

        var occupancy = exposure.NaicsCode == "531110" ? 0.95m : 1.35m;
        cumulativeFactor *= occupancy;
        factors.Add(new(1, "OCCUPANCY", occupancy, RoundPremium(basePremium * cumulativeFactor)));

        var protectionClass = exposure.ProtectionClass switch
        {
            <= 3 => 1.00m,
            <= 6 => 1.10m,
            <= 8 => 1.25m,
            _ => 1.50m
        };
        cumulativeFactor *= protectionClass;
        factors.Add(new(2, "PROTECTION_CLASS", protectionClass, RoundPremium(basePremium * cumulativeFactor)));

        var buildingAge = exposure.YearBuilt switch
        {
            >= 2010 => 1.00m,
            >= 1980 => 1.08m,
            _ => 1.20m
        };
        cumulativeFactor *= buildingAge;
        var premium = RoundPremium(basePremium * cumulativeFactor);
        factors.Add(new(3, "BUILDING_AGE", buildingAge, premium));

        return new RatedExposure(exposure.ExposureRef, baseRate, premium, factors);
    }
}
