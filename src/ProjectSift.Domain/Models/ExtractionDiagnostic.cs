using ProjectSift.Domain.Validation;

namespace ProjectSift.Domain.Models;

public sealed record ExtractionDiagnostic
{
    public ExtractionDiagnostic(string code, string message, int? position = null)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(code));
        Message = Guard.NotNullOrWhiteSpace(message, nameof(message));
        Position = Guard.NonNegative(position, nameof(position));
    }
    public string Code { get; }
    public string Message { get; }
    public int? Position { get; }
}
