using System.Reflection;
using ArchitecturePatterns.Testing;
using CleanArchitecture.Application;
using CleanArchitecture.Domain;
using Hexagonal.Application;

var tests = new List<TestCase>();
string[] cleanDomainForbidden = ["CleanArchitecture.Infrastructure", "CleanArchitecture.Api", "Microsoft.AspNetCore"];
string[] cleanApplicationForbidden = ["CleanArchitecture.Api"];
string[] hexApplicationForbidden = ["Hexagonal.Adapters.InMemory", "Hexagonal.Adapters.Sqlite", "Hexagonal.Api"];

for (var i = 1; i <= 8; i++)
    tests.Add(new("Architecture", $"clean domain dependency rule {i}", () => AssertNoReferences(typeof(CleanArchitectureDomainMarker).Assembly, cleanDomainForbidden)));
for (var i = 1; i <= 7; i++)
    tests.Add(new("Architecture", $"clean application dependency rule {i}", () => AssertNoReferences(typeof(CleanCommerce).Assembly, cleanApplicationForbidden)));
for (var i = 1; i <= 7; i++)
    tests.Add(new("Architecture", $"hexagonal application avoids adapters {i}", () => AssertNoReferences(typeof(ICheckoutUseCase).Assembly, hexApplicationForbidden)));

tests.Add(new("Architecture", "composition root is outside clean application", () => AssertNoReferences(typeof(CleanCommerce).Assembly, ["Microsoft.AspNetCore"])));

return await Check.Run(tests);

static Task AssertNoReferences(Assembly assembly, string[] forbidden)
{
    var refs = assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
    foreach (var rule in forbidden)
        Check.True(!refs.Any(x => x.Contains(rule, StringComparison.OrdinalIgnoreCase)), $"{assembly.GetName().Name} references {rule}");
    return Task.CompletedTask;
}
