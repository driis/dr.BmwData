using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

public record TokenRequest
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; init; }

    [JsonPropertyName("grant_type")]
    public string GrantType { get; init; } = "urn:ietf:params:oauth:grant-type:device_code";

    [JsonPropertyName("device_code")]
    public string DeviceCode { get; init; }

    [JsonPropertyName("code_verifier")]
    public string CodeVerifier { get; init; }
}
