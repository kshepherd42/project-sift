using ProjectSift.Domain.Validation;

namespace ProjectSift.Application.Models;

public sealed record PayloadInput
{
    public PayloadInput(int payloadIndex, ReadOnlyMemory<byte> content, string? payloadName = null)
    {
        PayloadIndex = Guard.NonNegative(payloadIndex, nameof(payloadIndex));
        Content = content;
        PayloadName = string.IsNullOrWhiteSpace(payloadName) ? null : payloadName;
    }
    public int PayloadIndex { get; }
    public ReadOnlyMemory<byte> Content { get; }
    public string? PayloadName { get; }
}
