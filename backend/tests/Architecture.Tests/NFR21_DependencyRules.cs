using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace Architecture.Tests;

public class NFR21_DependencyRules
{
    private static readonly Assembly[] DomainAssemblies =
    [
        typeof(Policy.Domain.AssemblyMarker).Assembly,
        typeof(Rules.Domain.AssemblyMarker).Assembly,
        typeof(Rating.Domain.AssemblyMarker).Assembly,
        typeof(Ledger.Domain.AssemblyMarker).Assembly
    ];

    private static readonly Assembly[] ApplicationAssemblies =
    [
        typeof(Policy.Application.AssemblyMarker).Assembly,
        typeof(Rules.Application.AssemblyMarker).Assembly,
        typeof(Rating.Application.AssemblyMarker).Assembly,
        typeof(Ledger.Application.AssemblyMarker).Assembly
    ];

    [Fact]
    public void NFR21_domain_projects_reference_the_bcl_only()
    {
        foreach (var assembly in DomainAssemblies)
        {
            var forbidden = assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? "")
                .Where(name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                    || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                    || name.StartsWith("Confluent", StringComparison.Ordinal)
                    || name.StartsWith("Platform", StringComparison.Ordinal))
                .ToArray();
            forbidden.ShouldBeEmpty(string.Join(", ", forbidden));

            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Confluent.Kafka", "Platform")
                .GetResult();
            result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypes ?? []));
        }
    }

    [Fact]
    public void NFR21_application_projects_reference_only_their_domain()
    {
        foreach (var assembly in ApplicationAssemblies)
        {
            var service = assembly.GetName().Name!.Split('.')[0];
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? "").ToArray();
            references.ShouldContain($"{service}.Domain");
            references.Where(name => name.StartsWith("Platform", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || name.Contains("Api", StringComparison.Ordinal)
                || name.Contains("Infrastructure", StringComparison.Ordinal))
                .ShouldBeEmpty();
        }
    }

    [Fact]
    public void NFR21_services_do_not_reference_each_other()
    {
        var services = new[] { "Policy", "Rules", "Rating", "Ledger" };
        var assemblies = DomainAssemblies.Concat(ApplicationAssemblies).Concat(
        [
            typeof(Policy.Infrastructure.PolicyDbContext).Assembly,
            typeof(Ledger.Infrastructure.LedgerDbContext).Assembly
        ]);

        foreach (var assembly in assemblies)
        {
            var owner = assembly.GetName().Name!.Split('.')[0];
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? "");
            foreach (var other in services.Where(service => service != owner))
            {
                references.ShouldNotContain(name => name.StartsWith(other + ".", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void NFR21_platform_does_not_reference_a_service_or_domain_type()
    {
        var platform = new[]
        {
            typeof(Platform.Tenancy.ITenantContext).Assembly,
            typeof(Platform.Web.ServiceDefaults).Assembly,
            typeof(Platform.Idempotency.AssemblyMarker).Assembly,
            typeof(Platform.Messaging.AssemblyMarker).Assembly
        };
        var domainWords = new[] { "Policy", "Premium", "Exposure", "Quote", "Account" };
        foreach (var assembly in platform)
        {
            assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? "")
                .ShouldNotContain(name => name.StartsWith("Policy.", StringComparison.Ordinal)
                    || name.StartsWith("Rules.", StringComparison.Ordinal)
                    || name.StartsWith("Rating.", StringComparison.Ordinal)
                    || name.StartsWith("Ledger.", StringComparison.Ordinal));

            foreach (var type in assembly.GetTypes())
            {
                domainWords.ShouldNotContain(type.Name);
            }
        }
    }

    [Fact]
    public void NFR21_only_the_apphost_references_aspire_hosting()
    {
        var production = DomainAssemblies.Concat(ApplicationAssemblies).Append(typeof(Platform.Web.ServiceDefaults).Assembly);
        foreach (var assembly in production)
        {
            assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? "")
                .ShouldNotContain(name => name.StartsWith("Aspire.Hosting", StringComparison.Ordinal));
        }

        typeof(ClpAppHost).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? "")
            .ShouldContain(name => name.StartsWith("Aspire.Hosting", StringComparison.Ordinal));
    }
}
