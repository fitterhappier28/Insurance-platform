namespace Rating.Domain;

public sealed record RatedFactor(int Step, string Code, decimal Value, decimal PremiumAfter);

public sealed record RatedExposure(
    string ExposureRef,
    decimal BaseRate,
    decimal Premium,
    IReadOnlyList<RatedFactor> Factors);

public sealed record RateSnapshot(
    string RateManualVersion,
    decimal TotalPremium,
    string Currency,
    IReadOnlyList<RatedExposure> Exposures);

public sealed class NotRateableException(string message) : Exception(message);

public sealed class UnsupportedRateManualException(string version)
    : Exception($"Rate Manual '{version}' is not published.");
