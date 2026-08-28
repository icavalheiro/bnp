namespace Bnp.Core.Documents;

public sealed record DocumentSummary(
    Guid Id,
    string Title,
    string IconKey,
    string ColorKey,
    int TabOrder,
    DateTimeOffset UpdatedAt);