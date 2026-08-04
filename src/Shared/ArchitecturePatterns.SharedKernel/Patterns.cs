namespace ArchitecturePatterns.SharedKernel;

public interface ICommand<TResult>;
public interface IQuery<TResult>;
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}

public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);
    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);
    public Specification<T> Not() => new NotSpecification<T>(this);
}

public sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate) => left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate);
}

public sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate) => left.IsSatisfiedBy(candidate) || right.IsSatisfiedBy(candidate);
}

public sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public override bool IsSatisfiedBy(T candidate) => !inner.IsSatisfiedBy(candidate);
}

public sealed class PendingOrdersSpecification : Specification<Order>
{
    public override bool IsSatisfiedBy(Order candidate) => candidate.Status == OrderStatus.Draft;
}

public sealed class ConfirmedOrdersSpecification : Specification<Order>
{
    public override bool IsSatisfiedBy(Order candidate) => candidate.Status == OrderStatus.Confirmed;
}

public sealed class OrdersByCustomerSpecification(Guid customerId) : Specification<Order>
{
    public override bool IsSatisfiedBy(Order candidate) => candidate.CustomerId == customerId;
}

public sealed class OrdersWithMinimumValueSpecification(decimal minimum) : Specification<Order>
{
    public override bool IsSatisfiedBy(Order candidate) => candidate.Total >= minimum;
}

public sealed class LowStockProductsSpecification(int threshold) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product candidate) => candidate.Stock <= threshold;
}

public interface IRepository<T> where T : Entity
{
    Task Add(T entity, CancellationToken cancellationToken);
    Task<T?> Get(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<T>> List(Specification<T>? specification, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<Result> Commit(CancellationToken cancellationToken);
}

public sealed class InMemoryRepository<T> : IRepository<T> where T : Entity
{
    private readonly Dictionary<Guid, T> _store = [];

    public Task Add(T entity, CancellationToken cancellationToken)
    {
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<T?> Get(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_store.TryGetValue(id, out var entity) ? entity : null);

    public Task<IReadOnlyCollection<T>> List(Specification<T>? specification, CancellationToken cancellationToken)
    {
        IEnumerable<T> query = _store.Values;
        if (specification is not null)
            query = query.Where(specification.IsSatisfiedBy);
        return Task.FromResult<IReadOnlyCollection<T>>(query.ToArray());
    }
}

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public int CommitCount { get; private set; }
    public Task<Result> Commit(CancellationToken cancellationToken)
    {
        CommitCount++;
        return Task.FromResult(Result.Success());
    }
}
