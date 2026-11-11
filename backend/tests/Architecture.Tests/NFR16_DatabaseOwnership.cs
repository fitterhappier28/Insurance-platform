using Aspire.Hosting.ApplicationModel;
using Shouldly;

namespace Architecture.Tests;

public class NFR16_DatabaseOwnership
{
    [Fact]
    public void NFR16_each_database_resource_has_exactly_one_referencing_service()
    {
        var app = ClpAppHost.CreateBuilder([]);
        var expected = new Dictionary<string, string>
        {
            ["policy-db"] = "policy-api",
            ["ledger-db"] = "ledger-api"
        };

        foreach (var (databaseName, owner) in expected)
        {
            var database = app.Resources.Single(resource => resource.Name == databaseName);
            var referencers = app.Resources
                .Where(resource => resource.Annotations.OfType<ResourceRelationshipAnnotation>()
                    .Any(annotation => annotation.Resource.Name == database.Name
                        && annotation.Type.Contains("Reference", StringComparison.OrdinalIgnoreCase)))
                .Select(resource => resource.Name)
                .ToArray();

            referencers.ShouldBe([owner]);
        }

        var stateless = new[] { "rules-api", "rating-api" };
        foreach (var name in stateless)
        {
            var service = app.Resources.Single(resource => resource.Name == name);
            service.Annotations.OfType<ResourceRelationshipAnnotation>()
                .Where(annotation => annotation.Resource.Name.EndsWith("-db", StringComparison.Ordinal))
                .ShouldBeEmpty();
        }
    }
}
