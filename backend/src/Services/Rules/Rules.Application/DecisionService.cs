using Rules.Domain;

namespace Rules.Application;

public sealed class DecisionService
{
    public DecisionResult Decide(DateOnly effectiveDate, IReadOnlyList<RiskExposure> exposures, string? rulesetVersion = null) =>
        RulesEvaluator.Decide(effectiveDate, exposures, rulesetVersion);
}
