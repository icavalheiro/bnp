using Bnp.Core.Documents;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Bnp.Persistence;

public sealed class SqliteDocumentRepository : IDocumentRepository
{
    private const string DefaultDocumentColor = "#5B6B82";

    private readonly SqliteConnection _connection;
    private bool _isInitialized;

    public SqliteDocumentRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false
        }.ToString());
    }

    public WorkspaceSnapshot Initialize(string initialDocumentTitle, string initialDocumentContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(initialDocumentTitle);
        ArgumentNullException.ThrowIfNull(initialDocumentContent);

        if (!_isInitialized)
        {
            _connection.Open();
            Execute("PRAGMA journal_mode = WAL;");
            Execute("PRAGMA synchronous = NORMAL;");
            Execute("PRAGMA busy_timeout = 2500;");
            ApplyMigrations();
            EnsureInitialDocument(initialDocumentTitle, initialDocumentContent);
            _isInitialized = true;
        }

        return LoadWorkspace();
    }

    public DocumentRecord CreateDocument(string title, string iconKey = "file-text")
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconKey);

        var now = DateTimeOffset.UtcNow;
        var document = new DocumentRecord(
            Guid.NewGuid(),
            title.Trim(),
            iconKey,
            DefaultDocumentColor,
            DocumentFormats.PlainTextV1,
            string.Empty,
            GetNextTabOrder(),
            now,
            now);

        InsertDocument(document);
        return document;
    }

    public DocumentRecord? GetDocument(Guid id)
    {
        EnsureInitialized();
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT id, title, icon_key, color_key, content_format, content, tab_order, created_at, updated_at
            FROM documents
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadDocument(reader) : null;
    }

    public void SaveDocument(DocumentRecord document)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(document);

        using var command = _connection.CreateCommand();
        command.CommandText = """
            UPDATE documents
            SET title = $title,
                icon_key = $iconKey,
                color_key = $colorKey,
                content_format = $contentFormat,
                content = $content,
                tab_order = $tabOrder,
                updated_at = $updatedAt
            WHERE id = $id;
            """;
        AddDocumentParameters(command, document);

        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException($"Document '{document.Id}' no longer exists.");
        }
    }

    public void SetActiveDocument(Guid id)
    {
        EnsureInitialized();
        if (GetDocument(id) is null)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "The active document must exist.");
        }

        using var command = _connection.CreateCommand();
        command.CommandText = """
            UPDATE workspace_state
            SET active_document_id = $id
            WHERE singleton_id = 1;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.ExecuteNonQuery();
    }

    public void SetSidebarCollapsed(bool isCollapsed)
    {
        EnsureInitialized();
        using var command = _connection.CreateCommand();
        command.CommandText = """
            UPDATE workspace_state
            SET sidebar_collapsed = $isCollapsed
            WHERE singleton_id = 1;
            """;
        command.Parameters.AddWithValue("$isCollapsed", isCollapsed ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public void SetEditorPreferences(string themeKey, string languageKey)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(themeKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageKey);
        using var command = _connection.CreateCommand();
        command.CommandText = """
            UPDATE workspace_state
            SET theme_key = $themeKey, language_key = $languageKey
            WHERE singleton_id = 1;
            """;
        command.Parameters.AddWithValue("$themeKey", themeKey);
        command.Parameters.AddWithValue("$languageKey", languageKey);
        command.ExecuteNonQuery();
    }

    public void CreateBackup(string destinationPath)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString());
        destination.Open();
        _connection.BackupDatabase(destination);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private void ApplyMigrations()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);

        if (version == 0)
        {
            Migration001.Apply(_connection);
            version = 1;
        }

        if (version == 1)
        {
            Migration002.Apply(_connection);
            version = 2;
        }

        if (version == 2)
        {
            Migration003.Apply(_connection);
            version = 3;
        }

        if (version != 3)
        {
            throw new NotSupportedException($"Database schema version {version} is not supported.");
        }
    }

    private void EnsureInitialDocument(string title, string content)
    {
        using var countCommand = _connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM documents;";
        if (Convert.ToInt32(countCommand.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var document = new DocumentRecord(
            Guid.NewGuid(),
            title,
            "file-text",
            DefaultDocumentColor,
            DocumentFormats.PlainTextV1,
            content,
            0,
            now,
            now);

        using var transaction = _connection.BeginTransaction();
        InsertDocument(document, transaction);
        using var stateCommand = _connection.CreateCommand();
        stateCommand.Transaction = transaction;
        stateCommand.CommandText = """
            UPDATE workspace_state
            SET active_document_id = $id
            WHERE singleton_id = 1;
            """;
        stateCommand.Parameters.AddWithValue("$id", document.Id.ToString("D"));
        stateCommand.ExecuteNonQuery();
        transaction.Commit();
    }

    private WorkspaceSnapshot LoadWorkspace()
    {
        var documents = new List<DocumentSummary>();
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, title, icon_key, color_key, tab_order, updated_at
                FROM documents
                ORDER BY tab_order;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                documents.Add(new DocumentSummary(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(5))));
            }
        }

        using var stateCommand = _connection.CreateCommand();
        stateCommand.CommandText = """
            SELECT active_document_id, sidebar_collapsed, theme_key, language_key
            FROM workspace_state
            WHERE singleton_id = 1;
            """;
        using var stateReader = stateCommand.ExecuteReader();
        if (!stateReader.Read())
        {
            throw new InvalidOperationException("Workspace state is missing.");
        }

        var activeId = stateReader.IsDBNull(0)
            ? documents[0].Id
            : Guid.Parse(stateReader.GetString(0));
        var isSidebarCollapsed = stateReader.GetInt32(1) != 0;
        var themeKey = stateReader.GetString(2);
        var languageKey = stateReader.GetString(3);
        stateReader.Close();

        var activeDocument = GetDocument(activeId) ?? GetDocument(documents[0].Id)
            ?? throw new InvalidOperationException("The workspace has no active document.");

        return new WorkspaceSnapshot(
            documents,
            activeDocument,
            isSidebarCollapsed,
            themeKey,
            languageKey);
    }

    private void InsertDocument(DocumentRecord document, SqliteTransaction? transaction = null)
    {
        using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO documents(
                id, title, icon_key, color_key, content_format, content, tab_order, created_at, updated_at)
            VALUES (
                $id, $title, $iconKey, $colorKey, $contentFormat, $content, $tabOrder, $createdAt, $updatedAt);
            """;
        AddDocumentParameters(command, document);
        command.ExecuteNonQuery();
    }

    private int GetNextTabOrder()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(tab_order), -1) + 1 FROM documents;";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private void Execute(string commandText)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("The repository has not been initialized.");
        }
    }

    private static DocumentRecord ReadDocument(SqliteDataReader reader)
    {
        return new DocumentRecord(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetInt32(6),
            DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(7)),
            DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(8)));
    }

    private static void AddDocumentParameters(SqliteCommand command, DocumentRecord document)
    {
        command.Parameters.AddWithValue("$id", document.Id.ToString("D"));
        command.Parameters.AddWithValue("$title", document.Title);
        command.Parameters.AddWithValue("$iconKey", document.IconKey);
        command.Parameters.AddWithValue("$colorKey", document.ColorKey);
        command.Parameters.AddWithValue("$contentFormat", document.ContentFormat);
        command.Parameters.AddWithValue("$content", document.Content);
        command.Parameters.AddWithValue("$tabOrder", document.TabOrder);
        command.Parameters.AddWithValue("$createdAt", document.CreatedAt.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("$updatedAt", document.UpdatedAt.ToUnixTimeMilliseconds());
    }
}