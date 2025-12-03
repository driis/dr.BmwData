using Microsoft.Extensions.Logging;

namespace dr.BmwData;

/// <summary>
/// File-based implementation of IRefreshTokenStore.
/// Stores the refresh token in a local file.
/// </summary>
public class FileRefreshTokenStore : IRefreshTokenStore
{
    private readonly string _filePath;
    private readonly ILogger<FileRefreshTokenStore>? _logger;

    /// <summary>
    /// Creates a new FileRefreshTokenStore with a custom file path.
    /// </summary>
    /// <param name="filePath">The path to the file where the refresh token will be stored.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    public FileRefreshTokenStore(string filePath, ILogger<FileRefreshTokenStore>? logger = null)
    {
        _filePath = filePath;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new FileRefreshTokenStore using the default file path.
    /// The default location is ~/.bmwdata/refresh_token in the user's home directory.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    public FileRefreshTokenStore(ILogger<FileRefreshTokenStore>? logger = null)
        : this(GetDefaultFilePath(), logger)
    {
    }

    public async Task<string?> GetAsync()
    {
        if (!File.Exists(_filePath))
        {
            _logger?.LogDebug("Refresh token file not found at {FilePath}", _filePath);
            return null;
        }

        try
        {
            var token = await File.ReadAllTextAsync(_filePath);
            var trimmedToken = token.Trim();

            if (string.IsNullOrEmpty(trimmedToken))
            {
                _logger?.LogDebug("Refresh token file is empty at {FilePath}", _filePath);
                return null;
            }

            _logger?.LogDebug("Loaded refresh token from {FilePath}", _filePath);
            return trimmedToken;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load refresh token from {FilePath}", _filePath);
            return null;
        }
    }

    public async Task SaveAsync(string refreshToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger?.LogDebug("Created directory {Directory}", directory);
            }

            await File.WriteAllTextAsync(_filePath, refreshToken);
            _logger?.LogInformation("Saved refresh token to {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save refresh token to {FilePath}", _filePath);
            throw;
        }
    }

    private static string GetDefaultFilePath()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".bmwdata", "refresh_token");
    }
}
