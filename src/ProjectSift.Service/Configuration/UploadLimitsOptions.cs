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
        set => _maxRequestBytes = Guard.Positive(value, nameof(MaxRequestBytes));
    }

    public long MaxFileBytes
    {
        get => _maxFileBytes;
        set => _maxFileBytes = Guard.Positive(value, nameof(MaxFileBytes));
    }

    public int MaxFileCount
    {
        get => _maxFileCount;
        set => _maxFileCount = Guard.Positive(value, nameof(MaxFileCount));
    }
}
