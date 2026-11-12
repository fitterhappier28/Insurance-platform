using Policy.Domain;
using Shouldly;

namespace Policy.Tests;

public sealed class FR01_ScheduleTests
{
    [Fact]
    public void FR01_reference_schedule_flattens_building_attributes_for_exposure()
    {
        var schedule = Schedule.Create(
            [new CoverageInput("BLDG", "5000")],
            [new LocationInput("loc-1", new Address("100 Bay St", "Tampa", "FL", "33602"), 5,
                [new BuildingInput("bldg-1", "JoistedMasonry", 1998, 10000,
                    [new UnitInput("unit-1", "101", 5000, "Retail")])])],
            [
                new ExposureInput("exp-1", "BLDG", "bldg-1", 500000m, "531110"),
                new ExposureInput("exp-2", "BLDG", "unit-1", 100000m, "722511")
            ],
            new DateOnly(2026, 12, 1));

        schedule.Exposures.Count.ShouldBe(2);
        var buildingExposure = schedule.Exposures[0];
        buildingExposure.LocationRef.ShouldBe("loc-1");
        buildingExposure.BuildingRef.ShouldBe("bldg-1");
        buildingExposure.UnitRef.ShouldBeNull();
        buildingExposure.ProtectionClass.ShouldBe(5);
        var unitExposure = schedule.Exposures[1];
        unitExposure.BuildingRef.ShouldBe("bldg-1");
        unitExposure.UnitRef.ShouldBe("unit-1");
        unitExposure.Construction.ShouldBe("JoistedMasonry");
        unitExposure.YearBuilt.ShouldBe(1998);
    }

    [Fact]
    public void FR01_duplicate_schedule_refs_are_rejected()
    {
        var duplicateLocations = new[]
        {
            new LocationInput("same-ref", AddressValue, 5, [Building()]),
            new LocationInput("same-ref", AddressValue, 5, [Building("bldg-2")])
        };

        Should.Throw<ScheduleValidationException>(() => Schedule.Create(
            [new CoverageInput("BLDG", "5000")], duplicateLocations,
            [new ExposureInput("exp-1", "BLDG", "bldg-1", 1000m, "531110")],
            new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void FR01_exposure_must_reference_a_building_or_unit_in_its_schedule()
    {
        Should.Throw<ScheduleValidationException>(() => Schedule.Create(
            [new CoverageInput("BLDG", "5000")],
            [new LocationInput("loc-1", AddressValue, 5, [Building()])],
            [new ExposureInput("exp-1", "BLDG", "missing", 1000m, "531110")],
            new DateOnly(2026, 12, 1)));
    }

    private static Address AddressValue => new("100 Bay St", "Tampa", "FL", "33602");
    private static BuildingInput Building(string reference = "bldg-1") =>
        new(reference, "JoistedMasonry", 1998, 10000, []);
}
