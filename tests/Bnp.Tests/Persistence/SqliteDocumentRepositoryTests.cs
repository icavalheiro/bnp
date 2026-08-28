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
}