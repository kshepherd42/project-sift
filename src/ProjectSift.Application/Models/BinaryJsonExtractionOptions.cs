using ProjectSift.Domain.Validation;

namespace ProjectSift.Application.Models;

public sealed record BinaryJsonExtractionOptions
{
    public const int DefaultMaxCandidateBytes = 1024 * 1024;
    public const int DefaultMaxNestingDepth = 64;
    public const int DefaultMaxPathsPerDocument = 4096;
    public const int DefaultMaxDocumentsPerPayload = 512;

    public BinaryJsonExtractionOptions(
        int maxCandidateBytes = DefaultMaxCandidateBytes,
        int maxNestingDepth = DefaultMaxNestingDepth,
        int maxPathsPerDocument = DefaultMaxPathsPerDocument,
        int maxDocumentsPerPayload = DefaultMaxDocumentsPerPayload)
    {
        MaxCandidateBytes = Guard.Positive(maxCandidateBytes, nameof(maxCandidateBytes));
        MaxNestingDepth = Guard.Positive(maxNestingDepth, nameof(maxNestingDepth));
        MaxPathsPerDocument = Guard.Positive(maxPathsPerDocument, nameof(maxPathsPerDocument));
        MaxDocumentsPerPayload = Guard.Positive(maxDocumentsPerPayload, nameof(maxDocumentsPerPayload));
    }

    public int MaxCandidateBytes { get; }
    public int MaxNestingDepth { get; }
    public int MaxPathsPerDocument { get; }
    public int MaxDocumentsPerPayload { get; }
}
