using ProjectSift.Domain;

namespace ProjectSift.Application;

public interface IDocumentExtractor
{
    Task<DocumentResult> ExtractAsync(DocumentCandidate candidate, CancellationToken cancellationToken = default);
}
