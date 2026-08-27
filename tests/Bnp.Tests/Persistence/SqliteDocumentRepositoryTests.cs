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
                var initialWorkspace = repository.Initialize();
                Assert.Single(initialWorkspace.Documents);
                Assert.Equal("Welcome", initialWorkspace.ActiveDocument.Title);

                var created = repository.CreateDocument("Architecture", "idea") with
                {
                    ContentFormat = DocumentFormats.AvaloniaRichEditorJsonV1,
                    Content = "{\"version\":1,\"blocks\":[]}",
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                repository.SaveDocument(created);
                repository.SetActiveDocument(created.Id);
                repository.SetSidebarCollapsed(true);
                createdId = created.Id;
            }

            using var restoredRepository = new SqliteDocumentRepository(databasePath);
            var restoredWorkspace = restoredRepository.Initialize();

            Assert.Equal(2, restoredWorkspace.Documents.Count);
            Assert.Equal(createdId, restoredWorkspace.ActiveDocument.Id);
            Assert.Equal("Architecture", restoredWorkspace.ActiveDocument.Title);
            Assert.Equal("idea", restoredWorkspace.ActiveDocument.IconKey);
            Assert.Equal(DocumentFormats.AvaloniaRichEditorJsonV1, restoredWorkspace.ActiveDocument.ContentFormat);
            Assert.True(restoredWorkspace.IsSidebarCollapsed);
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
            repository.Initialize();
            var now = DateTimeOffset.UtcNow;
            var unknown = new DocumentRecord(
                Guid.NewGuid(),
                "Unknown",
                "file-text",
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
}