namespace Rules.Domain;

public sealed record RiskExposure(
    string ExposureRef,
    string CoverageCode,
    decimal Limit,
    string NaicsCode,
    string Construction,
    int YearBuilt,
    int ProtectionClass);
