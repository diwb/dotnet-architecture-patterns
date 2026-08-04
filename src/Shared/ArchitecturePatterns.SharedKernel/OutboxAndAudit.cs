using System.Text.Json;

namespace ArchitecturePatterns.SharedKernel;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public string Payload { get; init; } = "{}";
    public string IdempotencyKey { get; init; } = string.Empty;
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextAttemptAt { get; set; }
}

public enum OutboxStatus
{
    Pending,
    Processing,
    Published,
    Failed
}

public interface IOutboxStore
{
    Task AddFromDomainEvents(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OutboxMessage>> List(CancellationToken cancellationToken);
    Task<Result> Dispatch(string? failEventType, CancellationToken cancellationToken);
}

public sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly List<OutboxMessage> _messages = [];
    private readonly Lock _gate = new();

    public Task AddFromDomainEvents(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            foreach (var domainEvent in events)
            {
                var eventType = domainEvent.GetType().Name;
                var message = new OutboxMessage
                {
                    EventType = eventType,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    IdempotencyKey = $"{eventType}:{domainEvent.Id}"
                };
                _messages.Add(message);
                _messages.Add(new OutboxMessage
                {
                    EventType = nameof(OutboxMessageCreated),
                    Payload = JsonSerializer.Serialize(new OutboxMessageCreated(Guid.NewGuid(), DateTimeOffset.UtcNow, message.Id, eventType)),
                    IdempotencyKey = $"{nameof(OutboxMessageCreated)}:{message.Id}"
                });
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<OutboxMessage>> List(CancellationToken cancellationToken)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyCollection<OutboxMessage>>(_messages.Select(Clone).ToArray());
    }

    public Task<Result> Dispatch(string? failEventType, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            foreach (var message in _messages.Where(x => x.Status is OutboxStatus.Pending or OutboxStatus.Failed))
            {
                message.Status = OutboxStatus.Processing;
                if (string.Equals(message.EventType, failEventType, StringComparison.Ordinal))
                {
                    message.Status = OutboxStatus.Failed;
                    message.RetryCount++;
                    message.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(Math.Min(message.RetryCount, 10));
                    continue;
                }

                message.Status = OutboxStatus.Published;
            }
        }

        return Task.FromResult(Result.Success());
    }

    private static OutboxMessage Clone(OutboxMessage source) => new()
    {
        Id = source.Id,
        EventType = source.EventType,
        Payload = source.Payload,
        IdempotencyKey = source.IdempotencyKey,
        Status = source.Status,
        RetryCount = source.RetryCount,
        CreatedAt = source.CreatedAt,
        NextAttemptAt = source.NextAttemptAt
    };
}

public sealed record AuditEntry(Guid Id, DateTimeOffset At, string Action, string SubjectId, string Details);

public interface IAuditTrail
{
    void Record(string action, string subjectId, string details);
    IReadOnlyCollection<AuditEntry> List();
}

public sealed class InMemoryAuditTrail : IAuditTrail
{
    private readonly List<AuditEntry> _entries = [];

    public void Record(string action, string subjectId, string details) =>
        _entries.Add(new AuditEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, action, subjectId, Redactor.Redact(details)));

    public IReadOnlyCollection<AuditEntry> List() => _entries.ToArray();
}

public static class Redactor
{
    private static readonly string[] SensitiveWords = ["secret", "token", "password", "api-key", "apikey"];

    public static string Redact(string value)
    {
        var redacted = value;
        foreach (var word in SensitiveWords)
            redacted = redacted.Replace(word, "[redacted]", StringComparison.OrdinalIgnoreCase);
        return redacted;
    }
}
