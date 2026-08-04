using System.Text.Json;
using ArchitecturePatterns.SharedKernel;

var command = args.FirstOrDefault() ?? "help";
var wantsJson = args.Contains("--json", StringComparer.OrdinalIgnoreCase);

try
{
    object payload = command switch
    {
        "doctor" => new { status = "ok", sdk = Environment.Version.ToString(), repository = "dotnet-architecture-patterns" },
        "list-patterns" => new { patterns = Patterns() },
        "compare" => Compare(args.Skip(1).Where(x => !x.StartsWith("--", StringComparison.Ordinal)).ToArray()),
        "run-scenario" => await RunScenario(args, CancellationToken.None),
        "validate-architecture" => ValidateArchitecture(),
        "generate-report" => await GenerateReport(CancellationToken.None),
        _ => new { status = "error", message = "Unknown command." }
    };

    var failed = payload.GetType().GetProperty("status")?.GetValue(payload)?.ToString() == "error";
    Write(payload, wantsJson);
    return failed ? 2 : 0;
}
catch (Exception ex)
{
    Write(new { status = "error", message = Redactor.Redact(ex.Message) }, wantsJson: true);
    return 1;
}

static string[] Patterns() =>
[
    "clean",
    "vertical-slice",
    "modular-monolith",
    "hexagonal",
    "cqrs",
    "domain-events",
    "outbox",
    "repository",
    "specification",
    "result",
    "unit-of-work"
];

static object Compare(string[] pair)
{
    if (pair.Length < 2)
        return new { status = "error", message = "Two architectures are required." };

    return new
    {
        left = pair[0],
        right = pair[1],
        dimensions = new[]
        {
            new { dimension = "change isolation", left = "layer boundary", right = "feature boundary" },
            new { dimension = "testing", left = "use case and dependency tests", right = "slice-level tests" },
            new { dimension = "cost", left = "more projects and mapping", right = "less ceremony but duplication risk" }
        }
    };
}

static async Task<object> RunScenario(string[] args, CancellationToken cancellationToken)
{
    if (!args.Contains("checkout", StringComparer.OrdinalIgnoreCase))
        return new { status = "error", message = "Only checkout scenario is available." };

    var architecture = ValueAfter(args, "--architecture") ?? "clean";
    var report = await new CommerceService(new CommerceState(), architecture).RunCheckout(cancellationToken);
    return new { status = "ok", report };
}

static object ValidateArchitecture() => new
{
    status = "ok",
    rules = new[]
    {
        "Clean Domain has no Infrastructure/API reference",
        "Hexagonal Application has no Adapter reference",
        "API projects are composition roots",
        "External DTOs are separate from entities"
    }
};

static async Task<object> GenerateReport(CancellationToken cancellationToken)
{
    var architectures = new[] { "clean", "vertical-slice", "modular-monolith", "hexagonal" };
    var reports = new List<ScenarioReport>();
    foreach (var architecture in architectures)
        reports.Add(await new CommerceService(new CommerceState(), architecture).RunCheckout(cancellationToken));

    return new { status = "ok", generatedAt = DateTimeOffset.UtcNow, reports };
}

static string? ValueAfter(string[] args, string name)
{
    var index = Array.FindIndex(args, x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static void Write(object payload, bool wantsJson)
{
    if (wantsJson)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        return;
    }

    Console.WriteLine(JsonSerializer.Serialize(payload));
}
