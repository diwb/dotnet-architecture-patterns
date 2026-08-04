namespace Hexagonal.Adapters.Sqlite;

public static class SqliteSchema
{
    public const string CreateOutbox = """
        create table if not exists outbox (
          id text primary key,
          event_type text not null,
          payload text not null,
          idempotency_key text not null unique,
          status text not null,
          retry_count integer not null,
          next_attempt_at text null
        );
        """;
}
