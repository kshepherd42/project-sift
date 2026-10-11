using System.Text.Json;
using ProjectSift.Application.Models;

namespace ProjectSift.Application.Abstractions;

public interface IPropertyPathCollector
{
    PropertyPathCollectionResult Collect(JsonElement root, CancellationToken cancellationToken = default);
}
