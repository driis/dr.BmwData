using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

public record CreateContainerRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("technicalDescriptors")] string[] TechnicalDescriptors);
