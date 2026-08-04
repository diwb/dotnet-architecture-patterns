using ArchitecturePatterns.SharedKernel;
using ArchitecturePatterns.Testing;

var tests = new List<TestCase>();
for (var i = 1; i <= 4; i++)
    tests.Add(new("Security", $"audit trail redacts secret {i}", () => { var audit = new InMemoryAuditTrail(); audit.Record("payment", "1", "token password secret"); var details = audit.List().Single().Details; Check.True(!details.Contains("token", StringComparison.OrdinalIgnoreCase), "token should not leak"); return Task.CompletedTask; }));
for (var i = 1; i <= 3; i++)
    tests.Add(new("Security", $"invalid payload returns controlled error {i}", async () => { var service = new CommerceService(new CommerceState(), "security"); var result = await service.CreateCustomer(new("", "invalid"), CancellationToken.None); Check.Equal(ErrorCodes.Validation, result.Error.Code, "validation code should be stable"); }));
for (var i = 1; i <= 3; i++)
    tests.Add(new("Security", $"external order response is dto {i}", async () => { var service = new CommerceService(new CommerceState(), "security"); var c = await service.CreateCustomer(new("Safe", "safe@example.test"), CancellationToken.None); var o = await service.CreateOrder(new(c.Value!.Id), CancellationToken.None); var response = await service.GetOrder(o.Value!.Id, CancellationToken.None); Check.True(response.Value is OrderResponse, "response should be DTO"); }));

return await Check.Run(tests);
