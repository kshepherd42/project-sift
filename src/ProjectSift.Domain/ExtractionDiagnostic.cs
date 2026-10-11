namespace ProjectSift.Domain;

public sealed record ExtractionDiagnostic
{
    public ExtractionDiagnostic(string code, string message, int? position = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Diagnostic code cannot be null, empty, or whitespace.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Diagnostic message cannot be null, empty, or whitespace.", nameof(message));
        }

        if (position is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Diagnostic position cannot be negative.");
        }

        Code = code;
        Message = message;
        Position = position;
    }

    public string Code { get; }

    public string Message { get; }

    public int? Position { get; }
}
