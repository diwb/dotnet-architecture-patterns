using ArchitecturePatterns.SharedKernel;

namespace Patterns.Cqrs;

public sealed record CreateOrderCommand(Guid CustomerId) : ICommand<Result<Order>>;
public sealed record GetOrderQuery(Guid OrderId) : IQuery<Result<OrderResponse>>;
