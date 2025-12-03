using dr.BmwData.Models;

namespace dr.BmwData;

public interface IAuthenticationService
{
    /// <summary>
    /// Checks if the interactive device flow is required to obtain a valid token.
    /// Returns true if no valid access token exists and no refresh token is available.
    /// Note: This property cannot check the refresh token store. Use RequiresInteractiveFlowAsync() instead.
    /// </summary>
    [Obsolete("Use RequiresInteractiveFlowAsync() instead to properly check the refresh token store.")]
    bool RequiresInteractiveFlow { get; }

    /// <summary>
    /// Checks if the interactive device flow is required to obtain a valid token.
    /// Returns true if no valid access token exists and no refresh token is available (including from the store).
    /// </summary>
    Task<bool> RequiresInteractiveFlowAsync();

    /// <summary>
    /// Gets a valid access token. If no token is available or the current token is expired,
    /// it will use the refresh token to obtain a new one. If no refresh token is configured,
    /// throws an InvalidOperationException with instructions to run the interactive device flow.
    /// </summary>
    Task<string> GetAccessTokenAsync();

    /// <summary>
    /// Initiates the OAuth 2.0 Device Code Flow for interactive authentication.
    /// </summary>
    Task<DeviceCodeResponse> InitiateDeviceFlowAsync(string scope = "authenticate_user openid cardata:api:read cardata:streaming:read");

    /// <summary>
    /// Polls for a token after initiating the device flow. Stores the token internally upon success.
    /// Uses the interval and expiration from the DeviceCodeResponse.
    /// Returns the refresh token so it can be saved for future use.
    /// </summary>
    Task<string> PollForTokenAsync(DeviceCodeResponse deviceCodeResponse);
}
