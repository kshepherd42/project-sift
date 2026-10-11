using ProjectSift.Domain.Validation;

namespace ProjectSift.Application.Models;

public sealed record PayloadInput
{
    public PayloadInput(int payloadIndex, ReadOnlyMemory<byte> content)
    {
        PayloadIndex = Guard.NonNegative(payloadIndex, nameof(payloadIndex));
        Content = content;
    }
    public int PayloadIndex { get; }
    public ReadOnlyMemory<byte> Content { get; }
}
