using Microsoft.Data.Sqlite;

namespace Bnp.Persistence;

internal static class Migration004
{
    public static void Apply(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ALTER TABLE workspace_state
            ADD COLUMN active_document_updated_at INTEGER NOT NULL DEFAULT 0;

            ALTER TABLE workspace_state
            ADD COLUMN sidebar_updated_at INTEGER NOT NULL DEFAULT 0;

            ALTER TABLE workspace_state
            ADD COLUMN theme_updated_at INTEGER NOT NULL DEFAULT 0;

            ALTER TABLE workspace_state
            ADD COLUMN language_updated_at INTEGER NOT NULL DEFAULT 0;

            UPDATE workspace_state
            SET active_document_updated_at = unixepoch('now') * 1000,
                sidebar_updated_at = unixepoch('now') * 1000,
                theme_updated_at = unixepoch('now') * 1000,
                language_updated_at = unixepoch('now') * 1000
            WHERE singleton_id = 1;

            DROP INDEX ix_documents_tab_order;
            CREATE INDEX ix_documents_tab_order ON documents(tab_order);

            PRAGMA user_version = 4;
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}