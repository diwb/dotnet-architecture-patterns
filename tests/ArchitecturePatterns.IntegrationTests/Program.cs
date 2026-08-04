using ArchitecturePatterns.SharedKernel;
using CleanArchitecture.Application;
using Hexagonal.Application;
using Xunit;

namespace ArchitecturePatterns.IntegrationTests;

public sealed class CheckoutIntegrationTests
{
    [Theory]
    [InlineData("clean", 1)]
    [InlineData("clean", 2)]
    [InlineData("clean", 3)]
    [InlineData("clean", 4)]
    [InlineData("clean", 5)]
    [InlineData("vertical-slice", 1)]
    [InlineData("vertical-slice", 2)]
    [InlineData("vertical-slice", 3)]
    [InlineData("vertical-slice", 4)]
    [InlineData("vertical-slice", 5)]
    [InlineData("modular-monolith", 1)]
    [InlineData("modular-monolith", 2)]
    [InlineData("modular-monolith", 3)]
    [InlineData("modular-monolith", 4)]
    [InlineData("modular-monolith", 5)]
    [InlineData("hexagonal", 1)]
    [InlineData("hexagonal", 2)]
    [InlineData("hexagonal", 3)]
    [InlineData("hexagonal", 4)]
    [InlineData("hexagonal", 5)]
    public async Task CheckoutScenarioPublishesOutbox(string architecture, int run)
    {
        var report = await new CommerceService(new CommerceState(), $"{architecture}-{run}").RunCheckout(CancellationToken.None);
        Assert.Equal($"{architecture}-{run}", report.Architecture);
        Assert.Equal(99.80m, report.Total);
        Assert.Equal(8, report.OutboxMessages);
        Assert.Equal(report.OutboxMessages, report.PublishedMessages);
    }

    [Fact]
    public async Task CleanUseCaseIsTestableWithoutApi()
    {
        var service = new CleanCommerce(new CommerceState());
        var customer = await service.CreateCustomer(new("Ada", "ada@example.test"), CancellationToken.None);
        Assert.True(customer.IsSuccess);
    }

    [Fact]
    public async Task HexagonalPortRunsThroughAbstraction()
    {
        ICheckoutUseCase port = new HexagonalCheckoutUseCase(new CommerceService(new CommerceState(), "hexagonal"));
        var report = await port.Run(CancellationToken.None);
        Assert.Equal("hexagonal", report.Architecture);
        Assert.True(report.PublishedMessages > 0);
    }

    [Fact]
    public async Task InvalidCustomerIsRejected()
    {
        var result = await new CommerceService(new CommerceState(), "clean").CreateCustomer(new("", "bad"), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
    }

    [Fact]
    public async Task MissingOrderReturnsNotFound()
    {
        var result = await new CommerceService(new CommerceState(), "clean").GetOrder(Guid.NewGuid(), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.NotFound, result.Error.Code);
    }
}


public sealed class CommerceServiceFailureIntegrationTests
{
    [Fact]
    public async Task CreateOrderRequiresExistingCustomer()
    {
        var service = new CommerceService(new CommerceState(), "integration");
        var result = await service.CreateOrder(new(Guid.NewGuid()), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task AddItemRequiresExistingOrder()
    {
        var service = new CommerceService(new CommerceState(), "integration");
        var result = await service.AddItem(Guid.NewGuid(), new(Guid.NewGuid(), "SKU", 1, 10m), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task ConfirmRequiresExistingOrder()
    {
        var service = new CommerceService(new CommerceState(), "integration");
        var result = await service.Confirm(Guid.NewGuid(), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task RegisterPaymentRequiresExistingOrder()
    {
        var service = new CommerceService(new CommerceState(), "integration");
        var result = await service.RegisterPayment(Guid.NewGuid(), new(10m, "ref"), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task RegisterPaymentBeforeConfirmIsDomainFailure()
    {
        var service = new CommerceService(new CommerceState(), "integration");
        var customer = await service.CreateCustomer(new("Ada", "ada@example.test"), CancellationToken.None);
        var order = await service.CreateOrder(new(customer.Value!.Id), CancellationToken.None);
        var result = await service.RegisterPayment(order.Value!.Id, new(10m, "ref"), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.DomainRule, result.Error.Code);
    }
}
