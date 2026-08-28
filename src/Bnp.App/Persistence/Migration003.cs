using Microsoft.Data.Sqlite;

namespace Bnp.Persistence;

internal static class Migration003
{
    public static void Apply(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ALTER TABLE workspace_state
            ADD COLUMN theme_key TEXT NOT NULL DEFAULT 'system';

            ALTER TABLE workspace_state
            ADD COLUMN language_key TEXT NOT NULL DEFAULT '';

            PRAGMA user_version = 3;
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}