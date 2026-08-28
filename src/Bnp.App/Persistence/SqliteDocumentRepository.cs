using Bnp.Core.Documents;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Bnp.Persistence;

public sealed class SqliteDocumentRepository : IDocumentRepository
{
    private const string DefaultDocumentColor = "#5B6B82";

    private readonly SqliteConnection _connection;
    private readonly TimeProvider _timeProvider;
    private bool _isInitialized;

    public event Action? Changed;

    public SqliteDocumentRepository(string databasePath, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _timeProvider = timeProvider ?? TimeProvider.System;

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

    public WorkspaceSnapshot GetWorkspace()
    {
        EnsureInitialized();
        return LoadWorkspace();
    }

    public DocumentRecord CreateDocument(string title, string iconKey = "file-text")
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconKey);

        var now = _timeProvider.GetUtcNow();
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
        Changed?.Invoke();
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

        Changed?.Invoke();
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
            SET active_document_id = $id,
                active_document_updated_at = $updatedAt
            WHERE singleton_id = 1;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$updatedAt", _timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        command.ExecuteNonQuery();
        Changed?.Invoke();
    }

    public void SetSidebarCollapsed(bool isCollapsed)
    {
        EnsureInitialized();
        using var command = _connection.CreateCommand();
        command.CommandText = """
            UPDATE workspace_state
            SET sidebar_collapsed = $isCollapsed,
                sidebar_updated_at = $updatedAt
            WHERE singleton_id = 1;
            """;
        command.Parameters.AddWithValue("$isCollapsed", isCollapsed ? 1 : 0);
        command.Parameters.AddWithValue("$updatedAt", _timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        command.ExecuteNonQuery();
        Changed?.Invoke();
    }

    public void SetEditorPreferences(string themeKey, string languageKey)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(themeKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageKey);
        using var command = _connection.CreateCommand();
        command.CommandText = """
            UPDATE workspace_state
            SET theme_key = $themeKey,
                language_key = $languageKey,
                theme_updated_at = $updatedAt,
                language_updated_at = $updatedAt
            WHERE singleton_id = 1;
            """;
        command.Parameters.AddWithValue("$themeKey", themeKey);
        command.Parameters.AddWithValue("$languageKey", languageKey);
        command.Parameters.AddWithValue("$updatedAt", _timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        command.ExecuteNonQuery();
        Changed?.Invoke();
    }

    public bool MergeFrom(string sourcePath)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString()))
        {
            source.Open();
            using var versionCommand = source.CreateCommand();
            versionCommand.CommandText = "PRAGMA user_version;";
            var version = Convert.ToInt32(versionCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (version != 4)
            {
                throw new NotSupportedException($"Cloud database schema version {version} is not supported.");
            }
        }

        using var attachCommand = _connection.CreateCommand();
        attachCommand.CommandText = "ATTACH DATABASE $sourcePath AS cloud;";
        attachCommand.Parameters.AddWithValue("$sourcePath", sourcePath);
        attachCommand.ExecuteNonQuery();
        var changed = false;
        try
        {
            using var transaction = _connection.BeginTransaction();
            using var documentsCommand = _connection.CreateCommand();
            documentsCommand.Transaction = transaction;
            documentsCommand.CommandText = """
                INSERT INTO documents(
                    id, title, icon_key, color_key, content_format, content,
                    tab_order, created_at, updated_at)
                SELECT id, title, icon_key, color_key, content_format, content,
                    tab_order, created_at, updated_at
                FROM cloud.documents
                WHERE true
                ON CONFLICT(id) DO UPDATE SET
                    title = excluded.title,
                    icon_key = excluded.icon_key,
                    color_key = excluded.color_key,
                    content_format = excluded.content_format,
                    content = excluded.content,
                    tab_order = excluded.tab_order,
                    created_at = excluded.created_at,
                    updated_at = excluded.updated_at
                WHERE excluded.updated_at > documents.updated_at;
                """;
            changed = documentsCommand.ExecuteNonQuery() > 0;

            using var workspaceCommand = _connection.CreateCommand();
            workspaceCommand.Transaction = transaction;
            workspaceCommand.CommandText = """
                UPDATE workspace_state
                SET active_document_id = CASE
                        WHEN cloud.active_document_updated_at > workspace_state.active_document_updated_at
                        THEN cloud.active_document_id ELSE workspace_state.active_document_id END,
                    active_document_updated_at = MAX(
                        workspace_state.active_document_updated_at, cloud.active_document_updated_at),
                    sidebar_collapsed = CASE
                        WHEN cloud.sidebar_updated_at > workspace_state.sidebar_updated_at
                        THEN cloud.sidebar_collapsed ELSE workspace_state.sidebar_collapsed END,
                    sidebar_updated_at = MAX(
                        workspace_state.sidebar_updated_at, cloud.sidebar_updated_at),
                    theme_key = CASE
                        WHEN cloud.theme_updated_at > workspace_state.theme_updated_at
                        THEN cloud.theme_key ELSE workspace_state.theme_key END,
                    theme_updated_at = MAX(
                        workspace_state.theme_updated_at, cloud.theme_updated_at),
                    language_key = CASE
                        WHEN cloud.language_updated_at > workspace_state.language_updated_at
                        THEN cloud.language_key ELSE workspace_state.language_key END,
                    language_updated_at = MAX(
                        workspace_state.language_updated_at, cloud.language_updated_at)
                FROM cloud.workspace_state AS cloud
                WHERE workspace_state.singleton_id = 1
                  AND cloud.singleton_id = 1
                  AND (cloud.active_document_updated_at > workspace_state.active_document_updated_at
                    OR cloud.sidebar_updated_at > workspace_state.sidebar_updated_at
                    OR cloud.theme_updated_at > workspace_state.theme_updated_at
                    OR cloud.language_updated_at > workspace_state.language_updated_at);
                """;
            changed |= workspaceCommand.ExecuteNonQuery() > 0;
            transaction.Commit();
        }
        finally
        {
            using var detachCommand = _connection.CreateCommand();
            detachCommand.CommandText = "DETACH DATABASE cloud;";
            detachCommand.ExecuteNonQuery();
        }

        if (changed)
        {
            Changed?.Invoke();
        }

        return changed;
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

        if (version == 3)
        {
            Migration004.Apply(_connection);
            version = 4;
        }

        if (version != 4)
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

        var now = _timeProvider.GetUtcNow();
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
            SET active_document_id = $id,
                active_document_updated_at = $updatedAt
            WHERE singleton_id = 1;
            """;
        stateCommand.Parameters.AddWithValue("$id", document.Id.ToString("D"));
        stateCommand.Parameters.AddWithValue("$updatedAt", now.ToUnixTimeMilliseconds());
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
                ORDER BY tab_order, id;
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