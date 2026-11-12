using Rules.Domain;
using Shouldly;

namespace Rules.Tests;

public sealed class FR04_DecisionTests
{
    private static readonly DateOnly EffectiveDate = new(2026, 7, 1);

    [Fact]
    public void FR04_reference_exposure_is_straight_through()
    {
        var result = RulesEvaluator.Decide(EffectiveDate, [ReferenceExposure()]);

        result.Outcome.ShouldBe(DecisionOutcome.StraightThrough);
        result.RulesetVersion.ShouldBe("CP-RULES-2026.07");
        result.Reasons.ShouldBeEmpty();
        result.Exposures.Single().Outcome.ShouldBe(DecisionOutcome.StraightThrough);
    }

    [Fact]
    public void FR04_unsupported_naics_declines_with_reason()
    {
        var result = RulesEvaluator.Decide(EffectiveDate, [ReferenceExposure() with { NaicsCode = "541110" }]);

        result.Outcome.ShouldBe(DecisionOutcome.Decline);
        result.Reasons.Select(reason => reason.Code).ShouldContain("D-NAICS-UNSUPPORTED");
        result.Reasons.Single().ExposureRef.ShouldBe("exp-1");
    }

    [Fact]
    public void FR04_protection_class_ten_declines()
    {
        var result = RulesEvaluator.Decide(EffectiveDate, [ReferenceExposure() with { ProtectionClass = 10 }]);

        result.Outcome.ShouldBe(DecisionOutcome.Decline);
        result.Reasons.Select(reason => reason.Code).ShouldContain("D-PROTECTION-CLASS");
    }

    [Fact]
    public void FR04_all_reasons_are_returned_and_decline_dominates_refer()
    {
        var result = RulesEvaluator.Decide(EffectiveDate,
        [
            ReferenceExposure() with { YearBuilt = 1972, Limit = 10_500_000m, ProtectionClass = 9 },
            ReferenceExposure("exp-2") with { NaicsCode = "541110" }
        ]);

        result.Outcome.ShouldBe(DecisionOutcome.Decline);
        result.Reasons.Select(reason => reason.Code).ShouldBe([
            "R-BUILDING-AGE", "R-LIMIT", "R-PROTECTION-CLASS", "D-NAICS-UNSUPPORTED"
        ]);
        result.Exposures.Select(exposure => exposure.Outcome).ShouldBe([
            DecisionOutcome.Refer, DecisionOutcome.Decline
        ]);
    }

    private static RiskExposure ReferenceExposure(string exposureRef = "exp-1") => new(
        exposureRef, "BLDG", 500_000m, "531110", "JoistedMasonry", 1998, 5);
}
