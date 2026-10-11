using System.Text.Json.Serialization;

namespace ProjectSift.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentStatus
{
    Successful,
    Recovered,
    Failed
}
