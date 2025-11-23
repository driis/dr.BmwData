using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace dr.BmwData.Tests.Mocks;

/// <summary>
/// Encapsulates WireMock server configuration for BMW authentication endpoints.
/// Provides methods to configure device code and token endpoint responses.
/// </summary>
public class BmwAuthMockServer : IDisposable
{
    private readonly WireMockServer _server;

    public string BaseUrl => _server.Url!;

    public BmwAuthMockServer()
    {
        _server = WireMockServer.Start();
    }

    /// <summary>
    /// Configures the device code endpoint to return a successful response.
    /// </summary>
    public void SetupDeviceCodeSuccess(string deviceCode, string userCode, int expiresIn = 300, int interval = 5)
    {
        _server
            .Given(Request.Create()
                .WithPath("/gcdm/oauth/device/code")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""device_code"": ""{deviceCode}"",
                    ""user_code"": ""{userCode}"",
                    ""verification_uri"": ""https://mock.bmw.com/verify"",
                    ""verification_uri_complete"": ""https://mock.bmw.com/verify?code={userCode}"",
                    ""expires_in"": {expiresIn},
                    ""interval"": {interval}
                }}"));
    }

    /// <summary>
    /// Configures the token endpoint to return a successful token response.
    /// </summary>
    public void SetupTokenSuccess(string accessToken, string refreshToken, int expiresIn = 3600)
    {
        _server
            .Given(Request.Create()
                .WithPath("/gcdm/oauth/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""access_token"": ""{accessToken}"",
                    ""token_type"": ""Bearer"",
                    ""expires_in"": {expiresIn},
                    ""refresh_token"": ""{refreshToken}"",
                    ""scope"": ""authenticate_user openid cardata:api:read""
                }}"));
    }

    /// <summary>
    /// Configures the token endpoint to return authorization_pending error.
    /// </summary>
    public void SetupTokenAuthorizationPending()
    {
        _server
            .Given(Request.Create()
                .WithPath("/gcdm/oauth/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""authorization_pending""}"));
    }

    /// <summary>
    /// Configures the token endpoint to return slow_down error.
    /// </summary>
    public void SetupTokenSlowDown()
    {
        _server
            .Given(Request.Create()
                .WithPath("/gcdm/oauth/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""slow_down""}"));
    }

    /// <summary>
    /// Configures the token endpoint to return a custom error.
    /// </summary>
    public void SetupTokenError(string error, string? errorDescription = null)
    {
        var body = errorDescription != null
            ? $@"{{""error"": ""{error}"", ""error_description"": ""{errorDescription}""}}"
            : $@"{{""error"": ""{error}""}}";

        _server
            .Given(Request.Create()
                .WithPath("/gcdm/oauth/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    /// <summary>
    /// Configures the token endpoint to return authorization_pending for the first N calls,
    /// then return a successful token response.
    /// </summary>
    public void SetupTokenPendingThenSuccess(int pendingCount, string accessToken, string refreshToken)
    {
        // First, set up pending responses
        for (int i = 0; i < pendingCount; i++)
        {
            _server
                .Given(Request.Create()
                    .WithPath("/gcdm/oauth/token")
                    .UsingPost())
                .InScenario("token-polling")
                .WillSetStateTo($"pending-{i + 1}")
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.BadRequest)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(@"{""error"": ""authorization_pending""}"));
        }

        // Then set up success response
        _server
            .Given(Request.Create()
                .WithPath("/gcdm/oauth/token")
                .UsingPost())
            .InScenario("token-polling")
            .WhenStateIs($"pending-{pendingCount}")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""access_token"": ""{accessToken}"",
                    ""token_type"": ""Bearer"",
                    ""expires_in"": 3600,
                    ""refresh_token"": ""{refreshToken}"",
                    ""scope"": ""authenticate_user openid cardata:api:read""
                }}"));
    }

    /// <summary>
    /// Configures the token endpoint to return slow_down error first,
    /// then return a successful token response.
    /// </summary>
    public void SetupTokenSlowDownThenSuccess(string accessToken, string refreshToken)
    {
        _server
            .Given(Request.Create()
                .WithPath("/gcdm/oauth/token")
                .UsingPost())
            .InScenario("slow-down-scenario")
            .WillSetStateTo("after-slow-down")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""slow_down""}"));

        _server
            .Given(Request.Create()
                .WithPath("/gcdm/oauth/token")
                .UsingPost())
            .InScenario("slow-down-scenario")
            .WhenStateIs("after-slow-down")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""access_token"": ""{accessToken}"",
                    ""token_type"": ""Bearer"",
                    ""expires_in"": 3600,
                    ""refresh_token"": ""{refreshToken}"",
                    ""scope"": ""authenticate_user openid cardata:api:read""
                }}"));
    }

    /// <summary>
    /// Resets all configured mappings.
    /// </summary>
    public void Reset()
    {
        _server.Reset();
    }

    public void Dispose()
    {
        _server?.Stop();
        _server?.Dispose();
    }
}
