using ProjectSift.Domain.Validation;

namespace ProjectSift.Service.Configuration;

public sealed class UploadLimitsOptions
{
    public const long DefaultMaxRequestBytes = 10 * 1024 * 1024;
    public const long DefaultMaxFileBytes = 2 * 1024 * 1024;
    public const int DefaultMaxFileCount = 32;

    private long _maxRequestBytes = DefaultMaxRequestBytes;
    private long _maxFileBytes = DefaultMaxFileBytes;
    private int _maxFileCount = DefaultMaxFileCount;

    public long MaxRequestBytes
    {
        get => _maxRequestBytes;
        set => _maxRequestBytes = ValidatePositive(value, nameof(MaxRequestBytes));
    }

    public long MaxFileBytes
    {
        get => _maxFileBytes;
        set => _maxFileBytes = ValidatePositive(value, nameof(MaxFileBytes));
    }

    public int MaxFileCount
    {
        get => _maxFileCount;
        set => _maxFileCount = Guard.Positive(value, nameof(MaxFileCount));
    }

    private static long ValidatePositive(long value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive.");
        }

        return value;
    }
}
