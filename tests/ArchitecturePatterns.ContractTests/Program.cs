using System.Text.Json;
using ArchitecturePatterns.Cli;
using ArchitecturePatterns.SharedKernel;
using Xunit;

namespace ArchitecturePatterns.ContractTests;

public sealed class ContractAndCliTests
{
    [Theory]
    [InlineData("contract-1")]
    [InlineData("contract-2")]
    [InlineData("contract-3")]
    [InlineData("contract-4")]
    [InlineData("contract-5")]
    public async Task ScenarioReportSerializesToStableJson(string architecture)
    {
        var report = await new CommerceService(new CommerceState(), architecture).RunCheckout(CancellationToken.None);
        var json = JsonSerializer.Serialize(report);
        Assert.Contains("Architecture", json, StringComparison.Ordinal);
        Assert.Contains("OutboxMessages", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CliDoctorReturnsJsonAndSuccess()
    {
        var result = await ArchToolApp.Execute(["doctor", "--json"], CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("ok", JsonDocument.Parse(result.Output).RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CliListPatternsIncludesRequiredArchitectures()
    {
        var result = await ArchToolApp.Execute(["list-patterns", "--json"], CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("clean", result.Output, StringComparison.Ordinal);
        Assert.Contains("hexagonal", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CliCompareReturnsTradeOffDimensions()
    {
        var result = await ArchToolApp.Execute(["compare", "clean", "vertical-slice", "--json"], CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("change isolation", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CliRunScenarioReturnsValidJson()
    {
        var result = await ArchToolApp.Execute(["run-scenario", "checkout", "--architecture", "clean", "--json"], CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("ok", JsonDocument.Parse(result.Output).RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CliValidateArchitectureCanReturnSuccess()
    {
        var result = await ArchToolApp.Execute(["validate-architecture", "--json"], CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Clean Domain", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CliValidateArchitectureViolationReturnsExitCodeOne()
    {
        var result = await ArchToolApp.Execute(["validate-architecture", "--fail", "--json"], CancellationToken.None);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Simulated architecture violation", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CliGenerateReportReturnsFourArchitectures()
    {
        var result = await ArchToolApp.Execute(["generate-report", "--json"], CancellationToken.None);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(4, json.RootElement.GetProperty("reports").GetArrayLength());
    }
}
