using ArchitecturePatterns.SharedKernel;
using Xunit;

namespace ArchitecturePatterns.UnitTests;

public sealed class UnitPatternTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void ResultSuccessPreservesValue(int value)
    {
        var result = Result.Success(value);
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void ResultFailureExposesStableError(int value)
    {
        var result = Result.Failure<int>(new Error(ErrorCodes.Validation, $"bad-{value}"));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void OrderAddItemCalculatesTotal(int quantity)
    {
        var order = new Order(Guid.NewGuid());
        var result = order.AddItem(Guid.NewGuid(), "SKU", quantity, 10m);
        Assert.True(result.IsSuccess);
        Assert.Equal(quantity * 10m, order.Total);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void OrderConfirmEmitsDomainEvent(int run)
    {
        var order = new Order(Guid.NewGuid());
        order.DequeueDomainEvents();
        order.AddItem(Guid.NewGuid(), $"SKU-{run}", 1, 10m);
        var result = order.Confirm();
        Assert.True(result.IsSuccess);
        Assert.Contains(order.DomainEvents, e => e is OrderConfirmed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void SpecificationsComposeBusinessPredicates(int quantity)
    {
        var order = new Order(Guid.NewGuid());
        order.AddItem(Guid.NewGuid(), "SKU", quantity, 20m);
        var minimum = new OrdersWithMinimumValueSpecification(quantity * 20m);
        Assert.True(new PendingOrdersSpecification().And(minimum).IsSatisfiedBy(order));
        Assert.False(new ConfirmedOrdersSpecification().Or(minimum.Not()).IsSatisfiedBy(order));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void ProductReserveUpdatesInventory(int quantity)
    {
        var product = new Product("SKU", "Product", 10m, 10);
        var result = product.Reserve(quantity);
        Assert.True(result.IsSuccess);
        Assert.Equal(10 - quantity, product.Stock);
    }

    [Theory]
    [InlineData("token")]
    [InlineData("password")]
    [InlineData("secret")]
    [InlineData("api-key")]
    [InlineData("APIKEY")]
    public void RedactorRemovesSensitiveWords(string word)
    {
        var text = Redactor.Redact($"value contains {word}");
        Assert.DoesNotContain(word, text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[redacted]", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task RepositoryStoresEntities(int run)
    {
        var repo = new InMemoryRepository<Customer>();
        var customer = new Customer($"Customer {run}", $"c{run}@example.test");
        await repo.Add(customer, CancellationToken.None);
        Assert.Equal(customer.Id, (await repo.Get(customer.Id, CancellationToken.None))!.Id);
    }
}

public sealed class DomainNegativePathTests
{
    [Fact]
    public void ProductReserveRejectsInvalidQuantity()
    {
        var product = new Product("SKU", "Product", 10m, 1);
        var result = product.Reserve(0);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
    }

    [Fact]
    public void ProductReserveRejectsInsufficientStock()
    {
        var product = new Product("SKU", "Product", 10m, 1);
        var result = product.Reserve(2);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Conflict, result.Error.Code);
    }

    [Fact]
    public void OrderConfirmRejectsEmptyOrder()
    {
        var order = new Order(Guid.NewGuid());
        var result = order.Confirm();
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.DomainRule, result.Error.Code);
    }

    [Fact]
    public void OrderAddItemRejectsConfirmedOrder()
    {
        var order = new Order(Guid.NewGuid());
        order.AddItem(Guid.NewGuid(), "SKU", 1, 10m);
        order.Confirm();
        var result = order.AddItem(Guid.NewGuid(), "SKU-2", 1, 10m);
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public void OrderAddItemRejectsInvalidValues(int quantity, decimal price)
    {
        var order = new Order(Guid.NewGuid());
        var result = order.AddItem(Guid.NewGuid(), "SKU", quantity, price);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
    }

    [Fact]
    public void PaymentRequiresConfirmedOrder()
    {
        var order = new Order(Guid.NewGuid());
        var result = order.RegisterPayment(10m, "ref");
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.DomainRule, result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public void PaymentRejectsInvalidAmount(decimal amount)
    {
        var order = new Order(Guid.NewGuid());
        order.AddItem(Guid.NewGuid(), "SKU", 1, 10m);
        order.Confirm();
        var result = order.RegisterPayment(amount, "ref");
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
    }

    [Fact]
    public async Task RepositoryListAppliesSpecification()
    {
        var repo = new InMemoryRepository<Order>();
        var customerId = Guid.NewGuid();
        var matching = new Order(customerId);
        var other = new Order(Guid.NewGuid());
        await repo.Add(matching, CancellationToken.None);
        await repo.Add(other, CancellationToken.None);
        var listed = await repo.List(new OrdersByCustomerSpecification(customerId), CancellationToken.None);
        Assert.Single(listed);
        Assert.Equal(matching.Id, listed.Single().Id);
    }

    [Fact]
    public async Task UnitOfWorkCountsCommits()
    {
        var uow = new InMemoryUnitOfWork();
        await uow.Commit(CancellationToken.None);
        await uow.Commit(CancellationToken.None);
        Assert.Equal(2, uow.CommitCount);
    }

    [Fact]
    public async Task OutboxDispatchMarksFailuresWithRetry()
    {
        var outbox = new InMemoryOutboxStore();
        var ev = new OrderCreated(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid());
        await outbox.AddFromDomainEvents([ev], CancellationToken.None);
        await outbox.Dispatch(nameof(OrderCreated), CancellationToken.None);
        var failed = (await outbox.List(CancellationToken.None)).Single(x => x.EventType == nameof(OrderCreated));
        Assert.Equal(OutboxStatus.Failed, failed.Status);
        Assert.Equal(1, failed.RetryCount);
        Assert.NotNull(failed.NextAttemptAt);
    }
}
