using System.Reflection;
using CleanArchitecture.Application;
using CleanArchitecture.Domain;
using Hexagonal.Application;
using Xunit;

namespace ArchitecturePatterns.ArchitectureTests;

public sealed class DependencyRuleTests
{
    private static readonly string[] CleanDomainForbidden = ["CleanArchitecture.Infrastructure", "CleanArchitecture.Api", "Microsoft.AspNetCore"];
    private static readonly string[] CleanApplicationForbidden = ["CleanArchitecture.Api"];
    private static readonly string[] HexApplicationForbidden = ["Hexagonal.Adapters.InMemory", "Hexagonal.Adapters.Sqlite", "Hexagonal.Api"];

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void CleanDomainDoesNotReferenceOuterLayers(int _) => AssertNoReferences(typeof(CleanArchitectureDomainMarker).Assembly, CleanDomainForbidden);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void CleanApplicationDoesNotReferenceApi(int _) => AssertNoReferences(typeof(CleanCommerce).Assembly, CleanApplicationForbidden);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void HexagonalApplicationDoesNotReferenceAdapters(int _) => AssertNoReferences(typeof(ICheckoutUseCase).Assembly, HexApplicationForbidden);

    [Fact]
    public void CompositionRootIsOutsideCleanApplication() => AssertNoReferences(typeof(CleanCommerce).Assembly, ["Microsoft.AspNetCore"]);

    private static void AssertNoReferences(Assembly assembly, string[] forbidden)
    {
        var refs = assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        foreach (var rule in forbidden)
            Assert.DoesNotContain(refs, x => x.Contains(rule, StringComparison.OrdinalIgnoreCase));
    }
}
