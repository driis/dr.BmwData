using dr.BmwData.Models;

namespace dr.BmwData.Tests.Mocks;

/// <summary>
/// Mock implementation of IAuthenticationService for testing.
/// Returns a configured access token without making real HTTP calls.
/// </summary>
public class MockAuthenticationService : IAuthenticationService
{
    private readonly string _accessToken;

    public MockAuthenticationService(string accessToken = "test-access-token")
    {
        _accessToken = accessToken;
    }

    public bool RequiresInteractiveFlow => false;

    public Task<string> GetAccessTokenAsync()
    {
        return Task.FromResult(_accessToken);
    }

    public Task<DeviceCodeResponse> InitiateDeviceFlowAsync(string scope = "authenticate_user openid cardata:api:read cardata:streaming:read")
    {
        throw new NotImplementedException("Use real AuthenticationService for device flow tests");
    }

    public Task PollForTokenAsync(DeviceCodeResponse deviceCodeResponse)
    {
        throw new NotImplementedException("Use real AuthenticationService for device flow tests");
    }
}
