using System.Text.Json;
using ArchitecturePatterns.SharedKernel;

namespace ArchitecturePatterns.Cli;

public static class ArchToolApp
{
    public static async Task<ArchToolResult> Execute(string[] args, CancellationToken cancellationToken)
    {
        var command = args.FirstOrDefault() ?? "help";
        try
        {
            object payload = command switch
            {
                "doctor" => new { status = "ok", sdk = Environment.Version.ToString(), repository = "dotnet-architecture-patterns" },
                "list-patterns" => new { status = "ok", patterns = Patterns() },
                "compare" => Compare(args.Skip(1).Where(x => !x.StartsWith("--", StringComparison.Ordinal)).ToArray()),
                "run-scenario" => await RunScenario(args, cancellationToken),
                "validate-architecture" => ValidateArchitecture(args),
                "generate-report" => await GenerateReport(cancellationToken),
                _ => new { status = "error", message = "Unknown command." }
            };

            var status = payload.GetType().GetProperty("status")?.GetValue(payload)?.ToString();
            var exitCode = status == "error" ? 1 : 0;
            return new ArchToolResult(exitCode, Serialize(payload, args.Contains("--json", StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception ex)
        {
            return new ArchToolResult(1, Serialize(new { status = "error", message = Redactor.Redact(ex.Message) }, wantsJson: true));
        }
    }

    public static string[] Patterns() =>
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

    private static object Compare(string[] pair)
    {
        if (pair.Length < 2)
            return new { status = "error", message = "Two architectures are required." };

        return new
        {
            status = "ok",
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

    private static async Task<object> RunScenario(string[] args, CancellationToken cancellationToken)
    {
        if (!args.Contains("checkout", StringComparer.OrdinalIgnoreCase))
            return new { status = "error", message = "Only checkout scenario is available." };

        var architecture = ValueAfter(args, "--architecture") ?? "clean";
        var report = await new CommerceService(new CommerceState(), architecture).RunCheckout(cancellationToken);
        return new { status = "ok", report };
    }

    private static object ValidateArchitecture(string[] args)
    {
        if (args.Contains("--fail", StringComparer.OrdinalIgnoreCase))
            return new { status = "error", message = "Simulated architecture violation." };

        return new
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
    }

    private static async Task<object> GenerateReport(CancellationToken cancellationToken)
    {
        var architectures = new[] { "clean", "vertical-slice", "modular-monolith", "hexagonal" };
        var reports = new List<ScenarioReport>();
        foreach (var architecture in architectures)
            reports.Add(await new CommerceService(new CommerceState(), architecture).RunCheckout(cancellationToken));

        return new { status = "ok", generatedAt = DateTimeOffset.UtcNow, reports };
    }

    private static string? ValueAfter(string[] args, string name)
    {
        var index = Array.FindIndex(args, x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string Serialize(object payload, bool wantsJson) =>
        JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = wantsJson });
}

public sealed record ArchToolResult(int ExitCode, string Output);
