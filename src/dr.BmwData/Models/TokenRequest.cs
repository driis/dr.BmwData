using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

public record TokenRequest(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("device_code")] string DeviceCode,
    [property: JsonPropertyName("code_verifier")] string CodeVerifier)
{
    [JsonPropertyName("grant_type")] public string GrantType { get; } = "urn:ietf:params:oauth:grant-type:device_code";
}
