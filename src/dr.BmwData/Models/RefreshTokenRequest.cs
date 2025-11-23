using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

public record RefreshTokenRequest(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("grant_type")] string GrantType = "refresh_token"
);
