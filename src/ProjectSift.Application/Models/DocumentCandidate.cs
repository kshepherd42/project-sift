using ProjectSift.Domain.Validation;

namespace ProjectSift.Application.Models;

public sealed record DocumentCandidate
{
    public DocumentCandidate(int payloadIndex, int documentIndex, ReadOnlyMemory<byte> content)
    {
        PayloadIndex = Guard.NonNegative(payloadIndex, nameof(payloadIndex));
        DocumentIndex = Guard.NonNegative(documentIndex, nameof(documentIndex));
        Content = content;
    }
    public int PayloadIndex { get; }
    public int DocumentIndex { get; }
    public ReadOnlyMemory<byte> Content { get; }
}
