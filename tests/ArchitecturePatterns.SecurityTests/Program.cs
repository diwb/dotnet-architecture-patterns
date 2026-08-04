using ArchitecturePatterns.SharedKernel;
using Xunit;

namespace ArchitecturePatterns.SecurityTests;

public sealed class SecurityBoundaryTests
{
    [Theory]
    [InlineData("token")]
    [InlineData("password")]
    [InlineData("secret")]
    [InlineData("api-key")]
    public void AuditTrailRedactsSecrets(string sensitiveWord)
    {
        var audit = new InMemoryAuditTrail();
        audit.Record("payment", "1", $"contains {sensitiveWord}");
        var details = audit.List().Single().Details;
        Assert.DoesNotContain(sensitiveWord, details, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("missing-at")]
    public async Task InvalidPayloadReturnsControlledError(string email)
    {
        var service = new CommerceService(new CommerceState(), "security");
        var result = await service.CreateCustomer(new("", email), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
    }

    [Theory]
    [InlineData("security-1")]
    [InlineData("security-2")]
    [InlineData("security-3")]
    public async Task ExternalOrderResponseIsDto(string architecture)
    {
        var service = new CommerceService(new CommerceState(), architecture);
        var customer = await service.CreateCustomer(new("Safe", "safe@example.test"), CancellationToken.None);
        var order = await service.CreateOrder(new(customer.Value!.Id), CancellationToken.None);
        var response = await service.GetOrder(order.Value!.Id, CancellationToken.None);
        Assert.IsType<OrderResponse>(response.Value);
    }
}
