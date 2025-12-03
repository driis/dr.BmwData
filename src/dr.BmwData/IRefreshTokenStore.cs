namespace dr.BmwData;

/// <summary>
/// Abstraction for persisting and loading refresh tokens.
/// Implement this interface to control how refresh tokens are stored.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// Loads the stored refresh token, if any.
    /// </summary>
    /// <returns>The refresh token, or null if none is stored.</returns>
    Task<string?> GetAsync();

    /// <summary>
    /// Saves the refresh token for future use.
    /// Called whenever a new refresh token is received from the BMW API.
    /// </summary>
    /// <param name="refreshToken">The refresh token to persist.</param>
    Task SaveAsync(string refreshToken);
}
