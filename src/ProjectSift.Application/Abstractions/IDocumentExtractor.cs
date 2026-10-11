using ProjectSift.Application.Models;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Abstractions;

public interface IDocumentExtractor
{
    Task<DocumentResult> ExtractAsync(DocumentCandidate candidate, CancellationToken cancellationToken = default);
}
