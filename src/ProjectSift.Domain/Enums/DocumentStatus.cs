using System.Text.Json.Serialization;

namespace ProjectSift.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentStatus
{
    Successful,
    Recovered,
    Failed
}
