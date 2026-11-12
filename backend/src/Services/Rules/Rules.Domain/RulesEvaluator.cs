namespace Rules.Domain;

public static class RulesEvaluator
{
    public const string Version = "CP-RULES-2026.07";
    public static readonly DateOnly EffectiveFrom = new(2026, 7, 1);

    public static DecisionResult Decide(
        DateOnly effectiveDate,
        IReadOnlyList<RiskExposure> exposures,
        string? rulesetVersion = null)
    {
        if (exposures.Count == 0)
        {
            throw new ArgumentException("At least one Exposure is required.", nameof(exposures));
        }

        if (rulesetVersion is not null && !string.Equals(rulesetVersion, Version, StringComparison.Ordinal))
        {
            throw new UnsupportedRulesetException(rulesetVersion);
        }

        if (effectiveDate < EffectiveFrom)
        {
            throw new UnsupportedRulesetException($"effective date {effectiveDate:yyyy-MM-dd}");
        }

        var reasons = new List<DecisionReason>();
        var exposureDecisions = new List<ExposureDecision>(exposures.Count);
        foreach (var exposure in exposures)
        {
            Validate(effectiveDate, exposure);
            var start = reasons.Count;
            if (exposure.NaicsCode is not ("531110" or "722511"))
            {
                reasons.Add(new("D-NAICS-UNSUPPORTED", "NAICS code is not supported by this Ruleset.", exposure.ExposureRef));
            }
            if (exposure.ProtectionClass == 10)
            {
                reasons.Add(new("D-PROTECTION-CLASS", "Protection class 10 is ineligible.", exposure.ExposureRef));
            }
            if (exposure.YearBuilt < 1980)
            {
                reasons.Add(new("R-BUILDING-AGE", "Building was built before 1980.", exposure.ExposureRef));
            }
            if (exposure.Limit > 10_000_000m)
            {
                reasons.Add(new("R-LIMIT", "Exposure limit exceeds 10,000,000.", exposure.ExposureRef));
            }
            if (exposure.ProtectionClass == 9)
            {
                reasons.Add(new("R-PROTECTION-CLASS", "Protection class 9 requires referral.", exposure.ExposureRef));
            }

            var outcome = OutcomeFor(reasons.Skip(start));
            exposureDecisions.Add(new(exposure.ExposureRef, outcome));
        }

        return new DecisionResult(
            exposureDecisions.MaxBy(decision => decision.Outcome)!.Outcome,
            Version,
            reasons,
            exposureDecisions);
    }

    private static void Validate(DateOnly effectiveDate, RiskExposure exposure)
    {
        if (string.IsNullOrWhiteSpace(exposure.ExposureRef) || exposure.ExposureRef.Length > 64
            || exposure.CoverageCode != "BLDG"
            || exposure.Limit is < 1m or > 25_000_000m
            || exposure.NaicsCode.Length != 6 || !exposure.NaicsCode.All(char.IsAsciiDigit)
            || exposure.Construction is not ("Frame" or "JoistedMasonry" or "NonCombustible" or "MasonryNonCombustible" or "FireResistive")
            || exposure.YearBuilt is < 1800 || exposure.YearBuilt > effectiveDate.Year
            || exposure.ProtectionClass is < 1 or > 10)
        {
            throw new ArgumentException($"Exposure '{exposure.ExposureRef}' has invalid attributes.", nameof(exposure));
        }
    }

    private static DecisionOutcome OutcomeFor(IEnumerable<DecisionReason> reasons)
    {
        var codes = reasons.Select(reason => reason.Code).ToHashSet(StringComparer.Ordinal);
        if (codes.Overlaps(["D-NAICS-UNSUPPORTED", "D-PROTECTION-CLASS"]))
        {
            return DecisionOutcome.Decline;
        }
        return codes.Overlaps(["R-BUILDING-AGE", "R-LIMIT", "R-PROTECTION-CLASS"])
            ? DecisionOutcome.Refer
            : DecisionOutcome.StraightThrough;
    }
}
