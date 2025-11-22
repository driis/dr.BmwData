using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

public record DeviceCodeRequest(string clientId, string scope, string codeChallenge)
{
    [JsonPropertyName("client_id")] public string ClientId { get; init; } = clientId;

    [JsonPropertyName("response_type")] public string ResponseType { get; init; } = "device_code";

    [JsonPropertyName("scope")] public string Scope { get; init; } = scope;

    [JsonPropertyName("code_challenge")] public string CodeChallenge { get; init; } = codeChallenge;

    [JsonPropertyName("code_challenge_method")] public string CodeChallengeMethod { get; init; } = "S256";
}
