namespace dr.BmwData.Tests.Mocks;

/// <summary>
/// Mock implementation of IRefreshTokenStore for testing.
/// Stores the refresh token in memory.
/// </summary>
public class MockRefreshTokenStore : IRefreshTokenStore
{
    private string? _refreshToken;

    public MockRefreshTokenStore(string? initialToken = null)
    {
        _refreshToken = initialToken;
    }

    public string? LastSavedToken { get; private set; }
    public int SaveCount { get; private set; }
    public int LoadCount { get; private set; }

    public Task<string?> GetAsync()
    {
        LoadCount++;
        return Task.FromResult(_refreshToken);
    }

    public Task SaveAsync(string refreshToken)
    {
        SaveCount++;
        LastSavedToken = refreshToken;
        _refreshToken = refreshToken;
        return Task.CompletedTask;
    }
}
