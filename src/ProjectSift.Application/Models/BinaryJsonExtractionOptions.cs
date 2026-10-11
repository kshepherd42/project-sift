using ProjectSift.Domain.Validation;

namespace ProjectSift.Application.Models;

public sealed record BinaryJsonExtractionOptions
{
    public const int DefaultMaxCandidateBytes = 1024 * 1024;
    public const int DefaultMaxNestingDepth = 64;
    public const int DefaultMaxPathsPerDocument = 4096;

    public BinaryJsonExtractionOptions(
        int maxCandidateBytes = DefaultMaxCandidateBytes,
        int maxNestingDepth = DefaultMaxNestingDepth,
        int maxPathsPerDocument = DefaultMaxPathsPerDocument)
    {
        if (maxCandidateBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCandidateBytes), "Value must be positive.");
        }

        if (maxNestingDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxNestingDepth), "Value must be positive.");
        }

        if (maxPathsPerDocument <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPathsPerDocument), "Value must be positive.");
        }

        MaxCandidateBytes = maxCandidateBytes;
        MaxNestingDepth = maxNestingDepth;
        MaxPathsPerDocument = maxPathsPerDocument;
    }

    public int MaxCandidateBytes { get; }
    public int MaxNestingDepth { get; }
    public int MaxPathsPerDocument { get; }
}
