using ProjectSift.Application.Models;

namespace ProjectSift.Application.Abstractions;

public interface IBinaryJsonBoundaryScanner
{
    BoundaryScanResult Scan(
        byte[] content,
        int offset,
        int contentLength,
        int startPosition,
        CancellationToken cancellationToken = default);
}
