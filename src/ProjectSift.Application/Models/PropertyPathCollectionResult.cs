namespace ProjectSift.Application.Models;

public sealed record PropertyPathCollectionResult(IReadOnlyCollection<string> Paths, bool Succeeded);
