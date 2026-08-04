namespace ArchitecturePatterns.SharedKernel;

public sealed record CreateCustomerRequest(string Name, string Email);
public sealed record CreateOrderRequest(Guid CustomerId);
public sealed record AddOrderItemRequest(Guid ProductId, string Sku, int Quantity, decimal UnitPrice);
public sealed record RegisterPaymentRequest(decimal Amount, string ProviderReference);
public sealed record OrderResponse(Guid Id, Guid CustomerId, string Status, decimal Total, IReadOnlyCollection<OrderItem> Items);

public sealed class CommerceState
{
    public IRepository<Customer> Customers { get; } = new InMemoryRepository<Customer>();
    public IRepository<Order> Orders { get; } = new InMemoryRepository<Order>();
    public IRepository<Product> Products { get; } = new InMemoryRepository<Product>();
    public IOutboxStore Outbox { get; } = new InMemoryOutboxStore();
    public IAuditTrail Audit { get; } = new InMemoryAuditTrail();
    public IUnitOfWork UnitOfWork { get; } = new InMemoryUnitOfWork();
}

public class CommerceService(CommerceState state, string architectureName)
{
    public string ArchitectureName => architectureName;

    public async Task<Result<Customer>> CreateCustomer(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || !request.Email.Contains('@', StringComparison.Ordinal))
            return Result.Failure<Customer>(new Error(ErrorCodes.Validation, "Customer name and email are required."));

        var customer = new Customer(request.Name, request.Email);
        await state.Customers.Add(customer, cancellationToken);
        state.Audit.Record("customer.created", customer.Id.ToString(), request.Email);
        await state.UnitOfWork.Commit(cancellationToken);
        return Result.Success(customer);
    }

    public async Task<Result<Order>> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (await state.Customers.Get(request.CustomerId, cancellationToken) is null)
            return Result.Failure<Order>(new Error(ErrorCodes.NotFound, "Customer was not found."));

        var order = new Order(request.CustomerId);
        await state.Orders.Add(order, cancellationToken);
        await state.Outbox.AddFromDomainEvents(order.DequeueDomainEvents(), cancellationToken);
        state.Audit.Record("order.created", order.Id.ToString(), "order created");
        await state.UnitOfWork.Commit(cancellationToken);
        return Result.Success(order);
    }

    public async Task<Result<Order>> AddItem(Guid orderId, AddOrderItemRequest request, CancellationToken cancellationToken)
    {
        var order = await state.Orders.Get(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<Order>(new Error(ErrorCodes.NotFound, "Order was not found."));

        var result = order.AddItem(request.ProductId, request.Sku, request.Quantity, request.UnitPrice);
        if (result.IsFailure)
            return Result.Failure<Order>(result.Error);

        state.Audit.Record("order.item-added", order.Id.ToString(), request.Sku);
        await state.UnitOfWork.Commit(cancellationToken);
        return Result.Success(order);
    }

    public async Task<Result<Order>> Confirm(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await state.Orders.Get(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<Order>(new Error(ErrorCodes.NotFound, "Order was not found."));

        var result = order.Confirm();
        if (result.IsFailure)
            return Result.Failure<Order>(result.Error);

        foreach (var item in order.Items)
            order.MarkInventoryReserved(item.ProductId, item.Quantity);
        await state.Outbox.AddFromDomainEvents(order.DequeueDomainEvents(), cancellationToken);
        state.Audit.Record("order.confirmed", order.Id.ToString(), "inventory reserved");
        await state.UnitOfWork.Commit(cancellationToken);
        return Result.Success(order);
    }

    public async Task<Result<Order>> RegisterPayment(Guid orderId, RegisterPaymentRequest request, CancellationToken cancellationToken)
    {
        var order = await state.Orders.Get(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<Order>(new Error(ErrorCodes.NotFound, "Order was not found."));

        var result = order.RegisterPayment(request.Amount, request.ProviderReference);
        if (result.IsFailure)
            return Result.Failure<Order>(result.Error);

        await state.Outbox.AddFromDomainEvents(order.DequeueDomainEvents(), cancellationToken);
        state.Audit.Record("payment.registered", order.Id.ToString(), Redactor.Redact(request.ProviderReference));
        await state.UnitOfWork.Commit(cancellationToken);
        return Result.Success(order);
    }

    public async Task<Result<OrderResponse>> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await state.Orders.Get(orderId, cancellationToken);
        return order is null
            ? Result.Failure<OrderResponse>(new Error(ErrorCodes.NotFound, "Order was not found."))
            : Result.Success(ToResponse(order));
    }

    public Task<IReadOnlyCollection<OutboxMessage>> GetOutbox(CancellationToken cancellationToken) =>
        state.Outbox.List(cancellationToken);

    public Task<Result> DispatchOutbox(CancellationToken cancellationToken) =>
        state.Outbox.Dispatch(null, cancellationToken);

    public async Task<ScenarioReport> RunCheckout(CancellationToken cancellationToken)
    {
        var customer = await CreateCustomer(new("Ada Lovelace", "ada@example.test"), cancellationToken);
        var order = await CreateOrder(new(customer.Value!.Id), cancellationToken);
        await AddItem(order.Value!.Id, new(Guid.NewGuid(), "SKU-ARCH-001", 2, 49.90m), cancellationToken);
        await Confirm(order.Value.Id, cancellationToken);
        await RegisterPayment(order.Value.Id, new(99.80m, "provider-ref-safe"), cancellationToken);
        await DispatchOutbox(cancellationToken);
        var outbox = await GetOutbox(cancellationToken);
        return new ScenarioReport(architectureName, order.Value.Id, order.Value.Total, outbox.Count, outbox.Count(x => x.Status == OutboxStatus.Published));
    }

    public static OrderResponse ToResponse(Order order) =>
        new(order.Id, order.CustomerId, order.Status.ToString(), order.Total, order.Items.ToArray());
}

public sealed record ScenarioReport(string Architecture, Guid OrderId, decimal Total, int OutboxMessages, int PublishedMessages);

