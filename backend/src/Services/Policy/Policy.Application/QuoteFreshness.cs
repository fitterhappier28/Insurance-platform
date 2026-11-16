using System.Text.Json.Nodes;

namespace Policy.Application;

public sealed record PolicyEvaluation(
    string Outcome,
    string RulesetVersion,
    string DecisionJson,
    string? RatingJson,
    string? RateManualVersion,
    decimal? WrittenPremium);

public static class QuoteFreshness
{
    public static bool HasExpired(DateTimeOffset expiresAt, DateTimeOffset now) => expiresAt <= now;

    public static bool IsPinnedVersionUnavailable(string code) =>
        code is "ruleset-unavailable" or "rate-manual-unavailable";

    public static bool IsStale(string storedDecisionJson, string? storedRatingJson, PolicyEvaluation evaluation) =>
        evaluation.Outcome != "StraightThrough"
        || evaluation.RatingJson is null
        || storedRatingJson is null
        || !JsonNode.DeepEquals(JsonNode.Parse(storedDecisionJson), JsonNode.Parse(evaluation.DecisionJson))
        || !JsonNode.DeepEquals(JsonNode.Parse(storedRatingJson), JsonNode.Parse(evaluation.RatingJson));
}
