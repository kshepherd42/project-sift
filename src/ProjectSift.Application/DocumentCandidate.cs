namespace ProjectSift.Application;

public sealed record DocumentCandidate
{
    public DocumentCandidate(int payloadIndex, int documentIndex, ReadOnlyMemory<byte> content)
    {
        PayloadIndex = Domain.Guard.NonNegative(payloadIndex, nameof(payloadIndex));
        DocumentIndex = Domain.Guard.NonNegative(documentIndex, nameof(documentIndex));
        Content = content;
    }
    public int PayloadIndex { get; }
    public int DocumentIndex { get; }
    public ReadOnlyMemory<byte> Content { get; }
}
