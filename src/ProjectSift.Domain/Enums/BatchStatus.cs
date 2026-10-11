using System.Text.Json.Serialization;

namespace ProjectSift.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BatchStatus
{
    Processing,
    Completed
}
