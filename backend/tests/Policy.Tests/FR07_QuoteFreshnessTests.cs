using Policy.Application;
using Shouldly;

namespace Policy.Tests;

public sealed class FR07_QuoteFreshnessTests
{
    [Theory]
    [InlineData("ruleset-unavailable")]
    [InlineData("rate-manual-unavailable")]
    public void FR07_unavailable_pinned_version_is_reported_as_stale(string dependencyCode)
    {
        QuoteFreshness.IsPinnedVersionUnavailable(dependencyCode).ShouldBeTrue();
        QuoteFreshness.IsPinnedVersionUnavailable("rating-unavailable").ShouldBeFalse();
    }

    [Fact]
    public void FR07_changed_reevaluation_is_stale_and_matching_reevaluation_is_not()
    {
        const string decision = """{"outcome":"StraightThrough","rulesetVersion":"CP-RULES-2026.07"}""";
        const string rating = """{"rateManualVersion":"CP-RATE-2026.07","writtenPremium":"1410.75"}""";
        var matching = new PolicyEvaluation("StraightThrough", "CP-RULES-2026.07", decision,
            rating, "CP-RATE-2026.07", 1410.75m);
        var changed = matching with
        {
            RatingJson = """{"rateManualVersion":"CP-RATE-2026.07","writtenPremium":"1450.00"}""",
            WrittenPremium = 1450.00m
        };

        QuoteFreshness.IsStale(decision, rating, matching).ShouldBeFalse();
        QuoteFreshness.IsStale(decision, rating, changed).ShouldBeTrue();
        QuoteFreshness.IsStale(decision, null, matching).ShouldBeTrue();
    }

    [Fact]
    public void FR07_quote_expiry_is_checked_against_the_bind_time()
    {
        var now = new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

        QuoteFreshness.HasExpired(now, now).ShouldBeTrue();
        QuoteFreshness.HasExpired(now.AddTicks(-1), now).ShouldBeTrue();
        QuoteFreshness.HasExpired(now.AddTicks(1), now).ShouldBeFalse();
    }
}
