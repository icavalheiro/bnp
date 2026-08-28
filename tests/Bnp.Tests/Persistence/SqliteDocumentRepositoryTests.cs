using Bnp.Core.Documents;
using Bnp.Persistence;

namespace Bnp.Tests.Persistence;

public sealed class SqliteDocumentRepositoryTests
{
    [Fact]
    public void InitializeRestoresDocumentsAndWorkspaceState()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "bnp-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(testDirectory, "library.db");

        try
        {
            Guid createdId;
            using (var repository = new SqliteDocumentRepository(databasePath))
            {
                var initialWorkspace = repository.Initialize("Welcome", "Welcome to BNP.");
                Assert.Single(initialWorkspace.Documents);
                Assert.Equal("Welcome", initialWorkspace.ActiveDocument.Title);

                var created = repository.CreateDocument("Architecture", "idea") with
                {
                    ColorKey = "#C43D4F",
                    ContentFormat = DocumentFormats.AvaloniaRichEditorJsonV1,
                    Content = "{\"version\":1,\"blocks\":[]}",
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                repository.SaveDocument(created);
                repository.SetActiveDocument(created.Id);
                repository.SetSidebarCollapsed(true);
                repository.SetEditorPreferences("dark", "pt");
                createdId = created.Id;
            }

            using var restoredRepository = new SqliteDocumentRepository(databasePath);
            var restoredWorkspace = restoredRepository.Initialize("Welcome", "Welcome to BNP.");

            Assert.Equal(2, restoredWorkspace.Documents.Count);
            Assert.Equal(createdId, restoredWorkspace.ActiveDocument.Id);
            Assert.Equal("Architecture", restoredWorkspace.ActiveDocument.Title);
            Assert.Equal("idea", restoredWorkspace.ActiveDocument.IconKey);
            Assert.Equal("#C43D4F", restoredWorkspace.ActiveDocument.ColorKey);
            Assert.Equal(DocumentFormats.AvaloniaRichEditorJsonV1, restoredWorkspace.ActiveDocument.ContentFormat);
            Assert.True(restoredWorkspace.IsSidebarCollapsed);
            Assert.Equal("dark", restoredWorkspace.ThemeKey);
            Assert.Equal("pt", restoredWorkspace.LanguageKey);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void SaveDocumentRejectsUnknownDocument()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "bnp-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(testDirectory, "library.db");

        try
        {
            using var repository = new SqliteDocumentRepository(databasePath);
            repository.Initialize("Welcome", "Welcome to BNP.");
            var now = DateTimeOffset.UtcNow;
            var unknown = new DocumentRecord(
                Guid.NewGuid(),
                "Unknown",
                "file-text",
                "#5B6B82",
                DocumentFormats.PlainTextV1,
                string.Empty,
                99,
                now,
                now);

            Assert.Throws<InvalidOperationException>(() => repository.SaveDocument(unknown));
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateBackupProducesRestorableSnapshotWhileRepositoryIsOpen()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "bnp-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(testDirectory, "library.db");
        var backupPath = Path.Combine(testDirectory, "backups", "library.db");

        try
        {
            using var repository = new SqliteDocumentRepository(databasePath);
            repository.Initialize("Welcome", "Welcome to BNP.");
            var created = repository.CreateDocument("Backed up document");

            repository.CreateBackup(backupPath);

            using var restoredRepository = new SqliteDocumentRepository(backupPath);
            var workspace = restoredRepository.Initialize("Other", "Other content");
            Assert.Contains(workspace.Documents, document => document.Id == created.Id);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void MergeFromUnitesDocumentsAndUsesLatestEntityTimestamps()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "bnp-tests", Guid.NewGuid().ToString("N"));
        var localPath = Path.Combine(testDirectory, "local.db");
        var cloudPath = Path.Combine(testDirectory, "cloud.db");
        var localClock = new MutableTimeProvider(new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));

        try
        {
            using var local = new SqliteDocumentRepository(localPath, localClock);
            local.Initialize("Welcome", "Initial");
            var shared = local.CreateDocument("Shared");
            local.CreateBackup(cloudPath);

            localClock.UtcNow = localClock.UtcNow.AddMinutes(1);
            local.SaveDocument(shared with
            {
                Title = "Local version",
                UpdatedAt = localClock.UtcNow
            });
            var localOnly = local.CreateDocument("Local only");
            local.SetEditorPreferences("light", "en");

            var cloudClock = new MutableTimeProvider(localClock.UtcNow.AddMinutes(1));
            Guid cloudOnlyId;
            using (var cloud = new SqliteDocumentRepository(cloudPath, cloudClock))
            {
                cloud.Initialize("Welcome", "Initial");
                cloud.SaveDocument(shared with
                {
                    Title = "Cloud version",
                    UpdatedAt = cloudClock.UtcNow
                });
                var cloudOnly = cloud.CreateDocument("Cloud only");
                cloudOnlyId = cloudOnly.Id;
                cloud.SetActiveDocument(cloudOnly.Id);
                cloud.SetSidebarCollapsed(true);
                cloud.SetEditorPreferences("dark", "pt");
            }

            Assert.True(local.MergeFrom(cloudPath));

            var merged = local.GetWorkspace();
            Assert.Contains(merged.Documents, document => document.Id == localOnly.Id);
            Assert.Contains(merged.Documents, document => document.Id == cloudOnlyId);
            Assert.Equal("Cloud version", local.GetDocument(shared.Id)?.Title);
            Assert.Equal(cloudOnlyId, merged.ActiveDocument.Id);
            Assert.True(merged.IsSidebarCollapsed);
            Assert.Equal("dark", merged.ThemeKey);
            Assert.Equal("pt", merged.LanguageKey);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}