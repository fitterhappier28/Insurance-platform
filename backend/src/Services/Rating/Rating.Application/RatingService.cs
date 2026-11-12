using Rating.Domain;

namespace Rating.Application;

public sealed class RatingService
{
    public RateSnapshot Rate(DateOnly effectiveDate, IReadOnlyList<RiskExposure> exposures, string? rateManualVersion = null) =>
        RateManual.Calculate(effectiveDate, exposures, rateManualVersion);
}
