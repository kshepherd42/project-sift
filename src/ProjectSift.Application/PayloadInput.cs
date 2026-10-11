namespace ProjectSift.Application;

public sealed record PayloadInput
{
    public PayloadInput(int payloadIndex, ReadOnlyMemory<byte> content)
    {
        PayloadIndex = Domain.Guard.NonNegative(payloadIndex, nameof(payloadIndex));
        Content = content;
    }
    public int PayloadIndex { get; }
    public ReadOnlyMemory<byte> Content { get; }
}
