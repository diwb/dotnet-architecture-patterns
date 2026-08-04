using ArchitecturePatterns.SharedKernel;
using ArchitecturePatterns.Testing;

var tests = new List<TestCase>();
foreach (var architecture in new[] { "clean", "vertical-slice", "modular-monolith", "hexagonal" })
{
    for (var i = 1; i <= 5; i++)
        tests.Add(new("Integration", $"checkout scenario publishes outbox {architecture} {i}", async () => { var report = await new CommerceService(new CommerceState(), architecture).RunCheckout(CancellationToken.None); Check.Equal(architecture, report.Architecture, "architecture should match"); Check.True(report.PublishedMessages >= 4, "outbox should publish events"); Check.Equal(99.80m, report.Total, "total should match"); }));
}

tests.Add(new("Integration", "invalid customer is rejected", async () => { var s = new CommerceService(new CommerceState(), "clean"); var r = await s.CreateCustomer(new("", "bad"), CancellationToken.None); Check.True(r.IsFailure, "invalid customer should fail"); }));
tests.Add(new("Integration", "missing order returns not found", async () => { var s = new CommerceService(new CommerceState(), "clean"); var r = await s.GetOrder(Guid.NewGuid(), CancellationToken.None); Check.Equal(ErrorCodes.NotFound, r.Error.Code, "missing order should be not found"); }));

return await Check.Run(tests);
