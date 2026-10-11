namespace ProjectSift.Application.Constants;

public static class ExtractionDiagnosticCodes
{
    public const string NoCandidatesDiscovered = "NO_CANDIDATES_DISCOVERED";
    public const string PayloadDocumentLimitExceeded = "PAYLOAD_DOCUMENT_LIMIT_EXCEEDED";
    public const string CandidateTooLarge = "CANDIDATE_TOO_LARGE";
    public const string CandidateDepthLimitExceeded = "CANDIDATE_DEPTH_LIMIT_EXCEEDED";
    public const string CandidateBoundaryMismatch = "CANDIDATE_BOUNDARY_MISMATCH";
    public const string CandidateIncompleteRecovered = "CANDIDATE_INCOMPLETE_RECOVERED";
    public const string CandidateIncompleteAmbiguous = "CANDIDATE_INCOMPLETE_AMBIGUOUS";
    public const string CandidateInvalidJson = "CANDIDATE_INVALID_JSON";
    public const string CandidateUnsupportedRoot = "CANDIDATE_UNSUPPORTED_ROOT";
    public const string CandidateEmpty = "CANDIDATE_EMPTY";
    public const string CandidatePathLimitExceeded = "CANDIDATE_PATH_LIMIT_EXCEEDED";
    public const string CandidateExtractionException = "CANDIDATE_EXTRACTION_EXCEPTION";
}
