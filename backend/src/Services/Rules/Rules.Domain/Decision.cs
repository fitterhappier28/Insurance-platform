namespace Rules.Domain;

public enum DecisionOutcome
{
    StraightThrough = 0,
    Refer = 1,
    Decline = 2
}

public sealed record DecisionReason(string Code, string Message, string ExposureRef);

public sealed record ExposureDecision(string ExposureRef, DecisionOutcome Outcome);

public sealed record DecisionResult(
    DecisionOutcome Outcome,
    string RulesetVersion,
    IReadOnlyList<DecisionReason> Reasons,
    IReadOnlyList<ExposureDecision> Exposures);

public sealed class UnsupportedRulesetException(string version)
    : Exception($"Ruleset '{version}' is not published.");
