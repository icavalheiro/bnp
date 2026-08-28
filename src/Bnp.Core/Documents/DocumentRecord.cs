namespace Bnp.Core.Documents;

public sealed record DocumentRecord(
    Guid Id,
    string Title,
    string IconKey,
    string ColorKey,
    string ContentFormat,
    string Content,
    int TabOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);