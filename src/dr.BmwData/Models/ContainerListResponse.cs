using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

public record ContainerListResponse(
    [property: JsonPropertyName("containers")] ContainerSummary[] Containers);

public record ContainerSummary(
    [property: JsonPropertyName("containerId")] string ContainerId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("state")] ContainerState State,
    [property: JsonPropertyName("created")] DateTime Created);
