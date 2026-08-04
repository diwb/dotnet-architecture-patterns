namespace ArchitecturePatterns.SharedKernel;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredAt { get; }
}

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public Guid Id { get; protected init; } = Guid.NewGuid();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public IDomainEvent[] DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}

public sealed record OrderCreated(Guid Id, DateTimeOffset OccurredAt, Guid OrderId, Guid CustomerId) : IDomainEvent;
public sealed record OrderConfirmed(Guid Id, DateTimeOffset OccurredAt, Guid OrderId) : IDomainEvent;
public sealed record PaymentRegistered(Guid Id, DateTimeOffset OccurredAt, Guid OrderId, decimal Amount) : IDomainEvent;
public sealed record InventoryReserved(Guid Id, DateTimeOffset OccurredAt, Guid OrderId, Guid ProductId, int Quantity) : IDomainEvent;
public sealed record OutboxMessageCreated(Guid Id, DateTimeOffset OccurredAt, Guid OutboxMessageId, string EventType) : IDomainEvent;

public sealed class Customer : Entity
{
    public Customer(string name, string email)
    {
        Name = name.Trim();
        Email = email.Trim();
    }

    public string Name { get; private set; }
    public string Email { get; private set; }
}

public sealed class Product : Entity
{
    public Product(string sku, string name, decimal price, int stock)
    {
        Sku = sku.Trim();
        Name = name.Trim();
        Price = price;
        Stock = stock;
    }

    public string Sku { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(new Error(ErrorCodes.Validation, "Quantity must be positive."));
        if (Stock < quantity)
            return Result.Failure(new Error(ErrorCodes.Conflict, "Insufficient stock."));
        Stock -= quantity;
        return Result.Success();
    }
}

public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = [];
    private readonly List<Payment> _payments = [];
    private readonly List<Shipment> _shipments = [];

    public Order(Guid customerId)
    {
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        Raise(new OrderCreated(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, customerId));
    }

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;
    public IReadOnlyCollection<Payment> Payments => _payments;
    public IReadOnlyCollection<Shipment> Shipments => _shipments;
    public decimal Total => _items.Sum(x => x.Total);

    public Result AddItem(Guid productId, string sku, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
            return Result.Failure(new Error(ErrorCodes.DomainRule, "Only draft orders can receive items."));
        if (quantity <= 0 || unitPrice <= 0)
            return Result.Failure(new Error(ErrorCodes.Validation, "Quantity and unit price must be positive."));

        _items.Add(new OrderItem(productId, sku, quantity, unitPrice));
        return Result.Success();
    }

    public Result Confirm()
    {
        if (_items.Count == 0)
            return Result.Failure(new Error(ErrorCodes.DomainRule, "Order must contain at least one item."));
        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmed(Guid.NewGuid(), DateTimeOffset.UtcNow, Id));
        return Result.Success();
    }

    public Result RegisterPayment(decimal amount, string providerReference)
    {
        if (Status != OrderStatus.Confirmed)
            return Result.Failure(new Error(ErrorCodes.DomainRule, "Only confirmed orders can be paid."));
        if (amount <= 0 || amount > Total)
            return Result.Failure(new Error(ErrorCodes.Validation, "Payment amount is invalid."));

        _payments.Add(new Payment(amount, providerReference));
        Raise(new PaymentRegistered(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, amount));
        return Result.Success();
    }

    public void MarkInventoryReserved(Guid productId, int quantity) =>
        Raise(new InventoryReserved(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, productId, quantity));
}

public sealed record OrderItem(Guid ProductId, string Sku, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

public sealed record Payment(decimal Amount, string ProviderReference);
public sealed record Shipment(string Carrier, string TrackingCode);

public enum OrderStatus
{
    Draft,
    Confirmed,
    Paid,
    Shipped,
    Cancelled
}
