using Rating.Domain;
using Shouldly;

namespace Rating.Tests;

public sealed class FR05_RatingTests
{
    [Fact]
    public void FR05_reference_exposure_has_deterministic_decimal_factor_steps()
    {
        var result = RateManual.Calculate(new DateOnly(2026, 7, 1), [ReferenceExposure()]);

        result.RateManualVersion.ShouldBe("CP-RATE-2026.07");
        result.TotalPremium.ShouldBe(1_410.75m);
        var exposure = result.Exposures.Single();
        exposure.BaseRate.ShouldBe(0.2500m);
        exposure.Premium.ShouldBe(1_410.75m);
        exposure.Factors.Select(factor => factor.Code).ShouldBe([
            "BASE_RATE", "OCCUPANCY", "PROTECTION_CLASS", "BUILDING_AGE"
        ]);
        exposure.Factors.Select(factor => factor.PremiumAfter).ShouldBe([
            1_250.00m, 1_187.50m, 1_306.25m, 1_410.75m
        ]);
    }

    [Fact]
    public void FR05_old_building_uses_age_factor_and_rounds_to_cents()
    {
        var result = RateManual.Calculate(new DateOnly(2026, 7, 1),
            [ReferenceExposure() with { YearBuilt = 1972 }]);

        result.TotalPremium.ShouldBe(1_567.50m);
        result.Exposures.Single().Factors.Last().Value.ShouldBe(1.20m);
    }

    [Theory]
    [InlineData("541110", 5)]
    [InlineData("531110", 10)]
    public void FR05_unrateable_exposure_is_rejected(string naicsCode, int protectionClass)
    {
        Should.Throw<NotRateableException>(() => RateManual.Calculate(new DateOnly(2026, 7, 1),
            [ReferenceExposure() with { NaicsCode = naicsCode, ProtectionClass = protectionClass }]));
    }

    [Fact]
    public void FR05_rounding_is_away_from_zero_at_a_half_cent()
    {
        RateManual.RoundPremium(1.005m).ShouldBe(1.01m);
    }

    private static RiskExposure ReferenceExposure() => new(
        "exp-1", "BLDG", 500_000m, "531110", "JoistedMasonry", 1998, 5);
}
