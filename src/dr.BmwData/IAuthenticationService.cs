using dr.BmwData.Models;

namespace dr.BmwData;

public interface IAuthenticationService
{
    Task<DeviceCodeResponse> InitiateDeviceFlowAsync(string scope = "authenticate_user openid cardata:api:read cardata:streaming:read");
    Task<TokenResponse> PollForTokenAsync(string clientId, string deviceCode, int interval, int expiresIn);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
}
