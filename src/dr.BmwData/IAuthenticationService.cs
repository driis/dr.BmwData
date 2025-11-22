using dr.BmwData.Models;

namespace dr.BmwData;

public interface IAuthenticationService
{
    Task<DeviceCodeResponse> InitiateDeviceFlowAsync(string clientId, string scope);
    Task<TokenResponse> PollForTokenAsync(string clientId, string deviceCode, int interval, int expiresIn);
}
