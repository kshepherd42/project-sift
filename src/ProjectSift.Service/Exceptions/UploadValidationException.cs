using ProjectSift.Service.Constants;

namespace ProjectSift.Service.Exceptions;

public sealed class UploadValidationException : Exception
{
    public IReadOnlyList<string> Issues { get; }

    public UploadValidationException(string message)
        : base(message)
    {
        Issues = [message];
    }

    public UploadValidationException(IReadOnlyList<string> issues)
        : base(issues.Count > 0 ? ServiceMessages.UploadValidationFailed : "Upload validation failed with no details.")
    {
        ArgumentNullException.ThrowIfNull(issues);
        Issues = issues.ToArray();
    }
}
