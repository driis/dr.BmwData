using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

public record ContainerResponse(
    [property: JsonPropertyName("containerId")] string ContainerId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("state")] ContainerState State,
    [property: JsonPropertyName("created")] DateTime Created,
    [property: JsonPropertyName("technicalDescriptors")] string[] TechnicalDescriptors);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContainerState
{
    ACTIVE,
    DELETED
}
