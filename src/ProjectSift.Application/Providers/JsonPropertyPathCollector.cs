using System.Text.Json;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Models;

namespace ProjectSift.Application.Providers;

public sealed class JsonPropertyPathCollector(BinaryJsonExtractionOptions options, IPropertyPathFormatter formatter) : IPropertyPathCollector
{
    private readonly BinaryJsonExtractionOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IPropertyPathFormatter _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

    public PropertyPathCollectionResult Collect(JsonElement root, CancellationToken cancellationToken = default)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var succeeded = CollectPaths(root, string.Empty, 0, paths, cancellationToken);
        return new PropertyPathCollectionResult(paths.ToArray(), succeeded);
    }

    private bool CollectPaths(JsonElement node, string currentPath, int depth, HashSet<string> paths, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (depth > _options.MaxNestingDepth)
        {
            return false;
        }

        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in node.EnumerateObject())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var propertyPath = _formatter.AppendPropertySegment(currentPath, property.Name);
                    if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        if (!CollectPaths(property.Value, propertyPath, depth + 1, paths, cancellationToken))
                        {
                            return false;
                        }

                        continue;
                    }

                    if (!TryAddPath(propertyPath, paths))
                    {
                        return false;
                    }
                }

                return true;

            case JsonValueKind.Array:
                var arrayPath = _formatter.AppendArraySegment(currentPath);
                if (!TryAddPath(arrayPath, paths))
                {
                    return false;
                }

                foreach (var item in node.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        if (!CollectPaths(item, arrayPath, depth + 1, paths, cancellationToken))
                        {
                            return false;
                        }
                    }
                }

                return true;

            default:
                if (!string.IsNullOrEmpty(currentPath) && !TryAddPath(currentPath, paths))
                {
                    return false;
                }

                return true;
        }
    }

    private bool TryAddPath(string path, HashSet<string> paths)
    {
        paths.Add(path);
        return paths.Count <= _options.MaxPathsPerDocument;
    }
}
