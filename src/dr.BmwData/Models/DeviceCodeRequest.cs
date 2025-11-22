using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

public record DeviceCodeRequest(
    [property: JsonPropertyName("client_id")] string ClientId, 
    [property: JsonPropertyName("scope")] string Scope, 
    [property: JsonPropertyName("code_challenge")] string CodeChallenge)
{
    [JsonPropertyName("response_type")] public string ResponseType { get; } = "device_code";
    [JsonPropertyName("code_challenge_method")] public string CodeChallengeMethod { get; } = "S256";
}
