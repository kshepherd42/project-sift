namespace ProjectSift.Application;

public sealed record PayloadInput
{
    public PayloadInput(int payloadIndex, ReadOnlyMemory<byte> content)
    {
        if (payloadIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadIndex), "Payload index cannot be negative.");
        }

        PayloadIndex = payloadIndex;
        Content = content;
    }

    public int PayloadIndex { get; }

    public ReadOnlyMemory<byte> Content { get; }
}
