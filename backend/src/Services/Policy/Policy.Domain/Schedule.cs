namespace Policy.Domain;

public sealed record CoverageInput(string Code, string Deductible);
public sealed record UnitInput(string Ref, string UnitNumber, int SquareFeet, string? Description = null);
public sealed record BuildingInput(string Ref, string Construction, int YearBuilt, int SquareFeet, IReadOnlyList<UnitInput> Units);
public sealed record LocationInput(string Ref, Address Address, int ProtectionClass, IReadOnlyList<BuildingInput> Buildings);
public sealed record ExposureInput(string Ref, string CoverageCode, string SubjectRef, decimal Limit, string NaicsCode);

public sealed record Coverage(Guid CoverageId, string Code, decimal Deductible);
public sealed record ScheduledUnit(Guid UnitId, string Ref, string UnitNumber, int SquareFeet, string? Description);
public sealed record ScheduledBuilding(Guid BuildingId, string Ref, string Construction, int YearBuilt, int SquareFeet,
    IReadOnlyList<ScheduledUnit> Units);
public sealed record ScheduledLocation(Guid LocationId, string Ref, Address Address, int ProtectionClass,
    IReadOnlyList<ScheduledBuilding> Buildings);
public sealed record ScheduledExposure(Guid ExposureId, string Ref, string CoverageCode, string SubjectRef,
    Guid LocationId, string LocationRef, Guid BuildingId, string BuildingRef, Guid? UnitId, string? UnitRef,
    decimal Limit, string NaicsCode, string Construction, int YearBuilt, int ProtectionClass);

public sealed record Schedule(
    IReadOnlyList<Coverage> Coverages,
    IReadOnlyList<ScheduledLocation> Locations,
    IReadOnlyList<ScheduledExposure> Exposures)
{
    private static readonly HashSet<string> Deductibles = new(StringComparer.Ordinal)
        { "1000", "2500", "5000", "10000", "25000" };

    public static Schedule Create(
        IReadOnlyList<CoverageInput> coverages,
        IReadOnlyList<LocationInput> locations,
        IReadOnlyList<ExposureInput> exposures,
        DateOnly effectiveFrom)
    {
        if (coverages.Count == 0 || locations.Count == 0 || exposures.Count == 0)
        {
            throw new ScheduleValidationException("At least one Coverage, Location and Exposure are required.");
        }

        var coverageCodes = new HashSet<string>(StringComparer.Ordinal);
        var builtCoverages = new List<Coverage>(coverages.Count);
        foreach (var coverage in coverages)
        {
            if (coverage.Code != "BLDG" || !Deductibles.Contains(coverage.Deductible)
                || !coverageCodes.Add(coverage.Code))
            {
                throw new ScheduleValidationException("Coverage code or deductible is invalid or duplicated.");
            }
            builtCoverages.Add(new(Guid.NewGuid(), coverage.Code, decimal.Parse(coverage.Deductible,
                System.Globalization.CultureInfo.InvariantCulture)));
        }

        var refs = new HashSet<string>(StringComparer.Ordinal);
        var builtLocations = new List<ScheduledLocation>(locations.Count);
        var subjects = new Dictionary<string, Subject>(StringComparer.Ordinal);
        foreach (var location in locations)
        {
            AddRef(refs, location.Ref);
            if (location.Address is null)
            {
                throw new ScheduleValidationException($"Location '{location.Ref}' must have an Address.");
            }
            location.Address.Validate();
            if (location.ProtectionClass is < 1 or > 10 || location.Buildings.Count == 0)
            {
                throw new ScheduleValidationException($"Location '{location.Ref}' is invalid or has no Building.");
            }

            var builtBuildings = new List<ScheduledBuilding>(location.Buildings.Count);
            foreach (var building in location.Buildings)
            {
                AddRef(refs, building.Ref);
                if (building.Construction is not ("Frame" or "JoistedMasonry" or "NonCombustible" or "MasonryNonCombustible" or "FireResistive")
                    || building.YearBuilt is < 1800 || building.YearBuilt > effectiveFrom.Year || building.SquareFeet <= 0)
                {
                    throw new ScheduleValidationException($"Building '{building.Ref}' is invalid.");
                }

                var scheduledUnits = new List<ScheduledUnit>(building.Units.Count);
                foreach (var unit in building.Units)
                {
                    AddRef(refs, unit.Ref);
                    if (string.IsNullOrWhiteSpace(unit.UnitNumber) || unit.UnitNumber.Length > 20 || unit.SquareFeet <= 0
                        || unit.Description?.Length > 200)
                    {
                        throw new ScheduleValidationException($"Unit '{unit.Ref}' is invalid.");
                    }
                    var scheduledUnit = new ScheduledUnit(Guid.NewGuid(), unit.Ref, unit.UnitNumber, unit.SquareFeet, unit.Description);
                    scheduledUnits.Add(scheduledUnit);
                    subjects.Add(unit.Ref, new(location, building, scheduledUnit));
                }

                var scheduledBuilding = new ScheduledBuilding(Guid.NewGuid(), building.Ref, building.Construction,
                    building.YearBuilt, building.SquareFeet, scheduledUnits);
                builtBuildings.Add(scheduledBuilding);
                subjects.Add(building.Ref, new(location, building, null));
            }

            builtLocations.Add(new ScheduledLocation(Guid.NewGuid(), location.Ref, location.Address,
                location.ProtectionClass, builtBuildings));
        }

        var builtExposures = new List<ScheduledExposure>(exposures.Count);
        foreach (var exposure in exposures)
        {
            AddRef(refs, exposure.Ref);
            if (!coverageCodes.Contains(exposure.CoverageCode)
                || !subjects.TryGetValue(exposure.SubjectRef, out var subject)
                || exposure.Limit is < 1m or > 25_000_000m
                || string.IsNullOrEmpty(exposure.NaicsCode) || exposure.NaicsCode.Length != 6
                || !exposure.NaicsCode.All(char.IsAsciiDigit))
            {
                throw new ScheduleValidationException($"Exposure '{exposure.Ref}' has invalid references or attributes.");
            }

            var location = builtLocations.Single(item => item.Ref == subject.Location.Ref);
            var building = location.Buildings.Single(item => item.Ref == subject.Building.Ref);
            var unit = subject.Unit is null ? null : building.Units.Single(item => item.Ref == subject.Unit.Ref);
            builtExposures.Add(new ScheduledExposure(Guid.NewGuid(), exposure.Ref, exposure.CoverageCode,
                exposure.SubjectRef, location.LocationId, location.Ref, building.BuildingId, building.Ref,
                unit?.UnitId, unit?.Ref, exposure.Limit, exposure.NaicsCode, building.Construction,
                building.YearBuilt, location.ProtectionClass));
        }

        return new Schedule(builtCoverages, builtLocations, builtExposures);
    }

    private static void AddRef(HashSet<string> refs, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || !refs.Add(value))
        {
            throw new ScheduleValidationException("Schedule refs must be non-empty, at most 64 characters, and unique.");
        }
    }

    private sealed record Subject(LocationInput Location, BuildingInput Building, ScheduledUnit? Unit);
}

public sealed class ScheduleValidationException(string message) : Exception(message);
