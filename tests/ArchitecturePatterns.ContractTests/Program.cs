using System.Text.Json;
using ArchitecturePatterns.SharedKernel;
using ArchitecturePatterns.Testing;

var tests = new List<TestCase>();
for (var i = 1; i <= 10; i++)
    tests.Add(new("Contract", $"scenario report serializes to stable json {i}", async () => { var report = await new CommerceService(new CommerceState(), "contract").RunCheckout(CancellationToken.None); var json = JsonSerializer.Serialize(report); Check.True(json.Contains("Architecture", StringComparison.Ordinal), "json should contain Architecture"); Check.True(json.Contains("OutboxMessages", StringComparison.Ordinal), "json should contain outbox count"); }));

return await Check.Run(tests);
