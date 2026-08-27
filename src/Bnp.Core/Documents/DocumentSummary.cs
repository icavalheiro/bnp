namespace Bnp.Core.Documents;

public sealed record DocumentSummary(
    Guid Id,
    string Title,
    string IconKey,
    int TabOrder,
    DateTimeOffset UpdatedAt);