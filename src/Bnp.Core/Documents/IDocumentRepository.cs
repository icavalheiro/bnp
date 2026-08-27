namespace Bnp.Core.Documents;

public interface IDocumentRepository : IDisposable
{
    WorkspaceSnapshot Initialize();

    DocumentRecord CreateDocument(string title, string iconKey = "file-text");

    DocumentRecord? GetDocument(Guid id);

    void SaveDocument(DocumentRecord document);

    void SetActiveDocument(Guid id);

    void SetSidebarCollapsed(bool isCollapsed);
}