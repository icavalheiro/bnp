using Microsoft.Data.Sqlite;

namespace Bnp.Persistence;

internal static class Migration002
{
    public static void Apply(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ALTER TABLE documents
            ADD COLUMN color_key TEXT NOT NULL DEFAULT '#5B6B82';

            PRAGMA user_version = 2;
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}