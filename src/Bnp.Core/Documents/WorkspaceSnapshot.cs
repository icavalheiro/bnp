namespace Bnp.Core.Documents;

public sealed record WorkspaceSnapshot(
    IReadOnlyList<DocumentSummary> Documents,
    DocumentRecord ActiveDocument,
    bool IsSidebarCollapsed,
    string ThemeKey,
    string LanguageKey);