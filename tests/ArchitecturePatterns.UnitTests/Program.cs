using ArchitecturePatterns.SharedKernel;
using ArchitecturePatterns.Testing;

var tests = new List<TestCase>();

for (var i = 1; i <= 10; i++)
    tests.Add(new("Unit", $"result success preserves value {i}", () => { var r = Result.Success(i); Check.True(r.IsSuccess, "result should succeed"); Check.Equal(i, r.Value, "value should match"); return Task.CompletedTask; }));

for (var i = 1; i <= 10; i++)
    tests.Add(new("Unit", $"result failure exposes error {i}", () => { var r = Result.Failure<int>(new Error(ErrorCodes.Validation, "bad")); Check.True(r.IsFailure, "result should fail"); Check.Equal(ErrorCodes.Validation, r.Error.Code, "code should match"); return Task.CompletedTask; }));

for (var i = 1; i <= 8; i++)
    tests.Add(new("Unit", $"order add item calculates total {i}", () => { var order = new Order(Guid.NewGuid()); var ok = order.AddItem(Guid.NewGuid(), "SKU", i, 10m); Check.True(ok.IsSuccess, "add item should succeed"); Check.Equal(i * 10m, order.Total, "total should match"); return Task.CompletedTask; }));

for (var i = 1; i <= 8; i++)
    tests.Add(new("Unit", $"order confirm emits event {i}", () => { var order = new Order(Guid.NewGuid()); order.DequeueDomainEvents(); order.AddItem(Guid.NewGuid(), "SKU", 1, 10m); var ok = order.Confirm(); Check.True(ok.IsSuccess, "confirm should succeed"); Check.True(order.DomainEvents.Any(e => e is OrderConfirmed), "event should be raised"); return Task.CompletedTask; }));

for (var i = 1; i <= 8; i++)
    tests.Add(new("Unit", $"specifications match expected orders {i}", () => { var order = new Order(Guid.NewGuid()); order.AddItem(Guid.NewGuid(), "SKU", i, 20m); var min = new OrdersWithMinimumValueSpecification(i * 20m); Check.True(min.IsSatisfiedBy(order), "minimum should match"); Check.True(new PendingOrdersSpecification().And(min).IsSatisfiedBy(order), "composed spec should match"); return Task.CompletedTask; }));

for (var i = 1; i <= 6; i++)
    tests.Add(new("Unit", $"product reserve updates inventory {i}", () => { var product = new Product("SKU", "Product", 10m, 10); var ok = product.Reserve(i); Check.True(ok.IsSuccess, "reserve should succeed"); Check.Equal(10 - i, product.Stock, "stock should decrease"); return Task.CompletedTask; }));

for (var i = 1; i <= 5; i++)
    tests.Add(new("Unit", $"redactor removes sensitive words {i}", () => { var text = Redactor.Redact("token password secret api-key"); Check.True(!text.Contains("token", StringComparison.OrdinalIgnoreCase), "token should be redacted"); Check.True(text.Contains("[redacted]", StringComparison.Ordinal), "marker should exist"); return Task.CompletedTask; }));

for (var i = 1; i <= 5; i++)
    tests.Add(new("Unit", $"repository stores entity {i}", async () => { var repo = new InMemoryRepository<Customer>(); var c = new Customer("A", "a@example.test"); await repo.Add(c, CancellationToken.None); Check.Equal(c.Id, (await repo.Get(c.Id, CancellationToken.None))!.Id, "repository should return entity"); }));

return await Check.Run(tests);
