namespace ProjectSift.Application.Constants;

public static class ExtractionMessages
{
    public const string CandidateContentEmpty = "Candidate content is empty.";
    public const string CandidateUnsupportedRoot = "Candidate root is neither a JSON object nor array.";
    public const string CandidateInvalidJson = "Candidate is not valid JSON (for example malformed syntax, truncation, or invalid UTF-8).";
    public const string CandidateBoundaryMismatch = "Candidate contains mismatched JSON delimiters.";
    public const string CandidateIncompleteAmbiguous = "Candidate ended in an ambiguous state (for example inside a JSON string) and was not repaired.";
    public const string NoCandidatesDiscovered = "No top-level JSON object or array candidate was discovered in the payload.";
    public const string UnexpectedBoundaryStatus = "Unexpected boundary status";

    public static string PayloadDocumentLimitExceeded(int maxDocumentsPerPayload)
        => $"Payload exceeded the configured document scan limit of {maxDocumentsPerPayload}.";

    public static string CandidateSizeLimitExceeded(int maxCandidateBytes)
        => $"Candidate exceeds the configured size limit of {maxCandidateBytes} bytes.";

    public static string CandidatePathLimitExceeded(int maxPathsPerDocument)
        => $"Candidate exceeded the configured path limit of {maxPathsPerDocument}.";

    public static string CandidateNestingLimitExceeded(int maxNestingDepth)
        => $"Candidate exceeded the configured nesting limit of {maxNestingDepth}.";

    public static string CandidateIncompleteRecovered()
        => "Candidate ended at payload boundary and was deterministically repaired by appending missing closing delimiters.";

    public static string CandidateExtractionException(string exceptionType)
        => $"Candidate extraction failed with an unexpected exception: {exceptionType}.";
}
