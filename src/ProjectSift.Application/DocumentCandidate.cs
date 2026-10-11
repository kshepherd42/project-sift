namespace ProjectSift.Application;

public sealed record DocumentCandidate
{
    public DocumentCandidate(int payloadIndex, int documentIndex, ReadOnlyMemory<byte> content)
    {
        if (payloadIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadIndex), "Payload index cannot be negative.");
        }

        if (documentIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentIndex), "Document index cannot be negative.");
        }

        PayloadIndex = payloadIndex;
        DocumentIndex = documentIndex;
        Content = content;
    }

    public int PayloadIndex { get; }

    public int DocumentIndex { get; }

    public ReadOnlyMemory<byte> Content { get; }
}
