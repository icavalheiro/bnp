using Microsoft.Data.Sqlite;

namespace Bnp.Persistence;

internal static class Migration001
{
    public static void Apply(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE documents (
                id TEXT PRIMARY KEY NOT NULL,
                title TEXT NOT NULL,
                icon_key TEXT NOT NULL,
                content_format TEXT NOT NULL,
                content TEXT NOT NULL,
                tab_order INTEGER NOT NULL,
                created_at INTEGER NOT NULL,
                updated_at INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX ix_documents_tab_order ON documents(tab_order);

            CREATE TABLE workspace_state (
                singleton_id INTEGER PRIMARY KEY NOT NULL CHECK (singleton_id = 1),
                active_document_id TEXT NULL,
                sidebar_collapsed INTEGER NOT NULL DEFAULT 0
            );

            INSERT INTO workspace_state(singleton_id, sidebar_collapsed)
            VALUES (1, 0);

            PRAGMA user_version = 1;
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}