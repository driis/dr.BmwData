using dr.BmwData.Models;
using dr.BmwData.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace dr.BmwData.Tests;

[TestFixture]
public class AuthenticationServiceTests
{
    private BmwAuthMockServer _mockServer = null!;
    private AuthenticationService _authService = null!;
    private BmwOptions _options = null!;

    // Low interval for fast tests (0 seconds = immediate polling)
    private const int TestIntervalSeconds = 0;

    [SetUp]
    public void Setup()
    {
        _mockServer = new BmwAuthMockServer();

        _options = new BmwOptions
        {
            ClientId = "test-client-id",
            DeviceFlowBaseUrl = _mockServer.BaseUrl,
            BaseUrl = "https://mock.bmw.com",
            SlowDownIncrementMs = 500     // Fast increment for tests
        };

        var httpClient = new HttpClient();
        var optionsWrapper = Options.Create(_options);
        var logger = NullLogger<AuthenticationService>.Instance;

        _authService = new AuthenticationService(httpClient, optionsWrapper, logger);
    }

    [TearDown]
    public void TearDown()
    {
        _mockServer?.Dispose();
    }

    /// <summary>
    /// Creates a DeviceCodeResponse for testing with a low interval for fast polling.
    /// </summary>
    private static DeviceCodeResponse CreateTestDeviceCodeResponse(
        string deviceCode,
        int expiresIn = 30,
        int interval = TestIntervalSeconds) =>
        new(
            DeviceCode: deviceCode,
            UserCode: "TEST-CODE",
            VerificationUri: "https://test.bmw.com/verify",
            VerificationUriComplete: "https://test.bmw.com/verify?code=TEST-CODE",
            ExpiresIn: expiresIn,
            Interval: interval);

    [Test]
    public async Task InitiateDeviceFlowAsync_Success_ReturnsDeviceCodeResponse()
    {
        // Arrange
        const string expectedDeviceCode = "test-device-code-123";
        const string expectedUserCode = "ABCD-1234";
        const int expectedExpiresIn = 300;
        const int expectedInterval = 5;

        _mockServer.SetupDeviceCodeSuccess(expectedDeviceCode, expectedUserCode, expectedExpiresIn, expectedInterval);

        // Act
        var result = await _authService.InitiateDeviceFlowAsync("test-scope");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DeviceCode, Is.EqualTo(expectedDeviceCode));
        Assert.That(result.UserCode, Is.EqualTo(expectedUserCode));
        Assert.That(result.ExpiresIn, Is.EqualTo(expectedExpiresIn));
        Assert.That(result.Interval, Is.EqualTo(expectedInterval));
        Assert.That(result.VerificationUri, Is.EqualTo("https://mock.bmw.com/verify"));
    }

    [Test]
    public async Task PollForTokenAsync_Success_StoresToken()
    {
        // Arrange
        const string expectedAccessToken = "test-access-token";
        const string expectedRefreshToken = "test-refresh-token";
        const string deviceCode = "device-123";

        // First initiate the flow to set up the code challenge
        _mockServer.SetupDeviceCodeSuccess(deviceCode, "USER-CODE");
        await _authService.InitiateDeviceFlowAsync("test-scope");

        // Then set up successful token response
        _mockServer.Reset();
        _mockServer.SetupTokenSuccess(expectedAccessToken, expectedRefreshToken);

        // Act
        var deviceCodeResponse = CreateTestDeviceCodeResponse(deviceCode, expiresIn: 10);
        await _authService.PollForTokenAsync(deviceCodeResponse);

        // Assert - verify token is stored by calling GetAccessTokenAsync
        var storedToken = await _authService.GetAccessTokenAsync();
        Assert.That(storedToken, Is.EqualTo(expectedAccessToken));
    }

    [Test]
    public async Task PollForTokenAsync_AuthorizationPending_ThenSuccess_StoresToken()
    {
        // Arrange
        const string expectedAccessToken = "test-access-token";
        const string expectedRefreshToken = "test-refresh-token";
        const string deviceCode = "device-123";

        // First initiate the flow
        _mockServer.SetupDeviceCodeSuccess(deviceCode, "USER-CODE");
        await _authService.InitiateDeviceFlowAsync("test-scope");

        // Set up pending then success scenario
        _mockServer.Reset();
        _mockServer.SetupTokenPendingThenSuccess(2, expectedAccessToken, expectedRefreshToken);

        // Act
        var deviceCodeResponse = CreateTestDeviceCodeResponse(deviceCode, expiresIn: 30);
        await _authService.PollForTokenAsync(deviceCodeResponse);

        // Assert - verify token is stored
        var storedToken = await _authService.GetAccessTokenAsync();
        Assert.That(storedToken, Is.EqualTo(expectedAccessToken));
    }

    [Test]
    public async Task PollForTokenAsync_SlowDown_IncreasesInterval()
    {
        // Arrange
        const string deviceCode = "device-123";
        const string expectedAccessToken = "access-token";
        const string expectedRefreshToken = "refresh-token";

        // First initiate the flow
        _mockServer.SetupDeviceCodeSuccess(deviceCode, "USER-CODE");
        await _authService.InitiateDeviceFlowAsync("test-scope");

        // Set up slow_down response first, then success
        _mockServer.Reset();
        _mockServer.SetupTokenSlowDownThenSuccess(expectedAccessToken, expectedRefreshToken);

        // Act - use 0 interval so the only delay comes from slow_down increment
        var deviceCodeResponse = CreateTestDeviceCodeResponse(deviceCode, expiresIn: 30, interval: 0);
        var startTime = DateTime.UtcNow;
        await _authService.PollForTokenAsync(deviceCodeResponse);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - verify token is stored and delay increased
        var storedToken = await _authService.GetAccessTokenAsync();
        Assert.That(storedToken, Is.EqualTo(expectedAccessToken));
        // Verify that the delay increased (0ms initial + 500ms after slow_down = 500ms minimum)
        Assert.That(elapsed.TotalMilliseconds, Is.GreaterThan(400));
    }

    [Test]
    public async Task PollForTokenAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        const string deviceCode = "device-123";

        // First initiate the flow
        _mockServer.SetupDeviceCodeSuccess(deviceCode, "USER-CODE");
        await _authService.InitiateDeviceFlowAsync("test-scope");

        // Set up authorization_pending to simulate timeout
        _mockServer.Reset();
        _mockServer.SetupTokenAuthorizationPending();

        // Act & Assert - use short expiry to trigger timeout quickly
        var deviceCodeResponse = CreateTestDeviceCodeResponse(deviceCode, expiresIn: 1, interval: 0);
        var exception = Assert.ThrowsAsync<TimeoutException>(async () =>
            await _authService.PollForTokenAsync(deviceCodeResponse));
        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    public async Task PollForTokenAsync_Error_ThrowsException()
    {
        // Arrange
        const string deviceCode = "device-123";
        const string expectedError = "invalid_grant";

        // First initiate the flow
        _mockServer.SetupDeviceCodeSuccess(deviceCode, "USER-CODE");
        await _authService.InitiateDeviceFlowAsync("test-scope");

        // Set up error response
        _mockServer.Reset();
        _mockServer.SetupTokenError(expectedError, "The device code is invalid");

        // Act & Assert
        var deviceCodeResponse = CreateTestDeviceCodeResponse(deviceCode, expiresIn: 10);
        var ex = Assert.ThrowsAsync<Exception>(async () =>
            await _authService.PollForTokenAsync(deviceCodeResponse));

        Assert.That(ex.Message, Does.Contain("Token polling failed"));
        Assert.That(ex.Message, Does.Contain(expectedError));
    }

    [Test]
    public void PollForTokenAsync_WithoutInitiate_ThrowsInvalidOperationException()
    {
        // Arrange
        var deviceCodeResponse = CreateTestDeviceCodeResponse("device-123", expiresIn: 10);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.PollForTokenAsync(deviceCodeResponse));
    }

    [Test]
    public void RequiresInteractiveFlow_NoTokenAndNoRefreshToken_ReturnsTrue()
    {
        // Arrange - service has no token and no configured refresh token

        // Act
        var result = _authService.RequiresInteractiveFlow;

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void RequiresInteractiveFlow_WithConfiguredRefreshToken_ReturnsFalse()
    {
        // Arrange - service with configured refresh token
        var optionsWithRefreshToken = new BmwOptions
        {
            ClientId = "test-client-id",
            DeviceFlowBaseUrl = _mockServer.BaseUrl,
            RefreshToken = "configured-refresh-token",
            SlowDownIncrementMs = 500
        };

        var httpClient = new HttpClient();
        var optionsWrapper = Options.Create(optionsWithRefreshToken);
        var logger = NullLogger<AuthenticationService>.Instance;
        var authServiceWithRefresh = new AuthenticationService(httpClient, optionsWrapper, logger);

        // Act
        var result = authServiceWithRefresh.RequiresInteractiveFlow;

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RequiresInteractiveFlow_AfterDeviceFlow_ReturnsFalse()
    {
        // Arrange - complete device flow to store token
        const string deviceCode = "device-123";
        _mockServer.SetupDeviceCodeSuccess(deviceCode, "USER-CODE");
        await _authService.InitiateDeviceFlowAsync("test-scope");

        _mockServer.Reset();
        _mockServer.SetupTokenSuccess("access-token", "refresh-token");
        var deviceCodeResponse = CreateTestDeviceCodeResponse(deviceCode, expiresIn: 10);
        await _authService.PollForTokenAsync(deviceCodeResponse);

        // Act
        var result = _authService.RequiresInteractiveFlow;

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetAccessTokenAsync_AfterDeviceFlow_ReturnsStoredToken()
    {
        // Arrange
        const string expectedAccessToken = "test-access-token";
        const string expectedRefreshToken = "test-refresh-token";
        const string deviceCode = "device-123";

        // Complete device flow
        _mockServer.SetupDeviceCodeSuccess(deviceCode, "USER-CODE");
        await _authService.InitiateDeviceFlowAsync("test-scope");

        _mockServer.Reset();
        _mockServer.SetupTokenSuccess(expectedAccessToken, expectedRefreshToken);
        var deviceCodeResponse = CreateTestDeviceCodeResponse(deviceCode, expiresIn: 10);
        await _authService.PollForTokenAsync(deviceCodeResponse);

        // Act
        var result = await _authService.GetAccessTokenAsync();

        // Assert
        Assert.That(result, Is.EqualTo(expectedAccessToken));
    }

    [Test]
    public async Task GetAccessTokenAsync_WithConfiguredRefreshToken_RefreshesAndReturnsToken()
    {
        // Arrange
        const string expectedAccessToken = "new-access-token";
        const string expectedNewRefreshToken = "new-refresh-token";

        // Create service with configured refresh token
        var optionsWithRefreshToken = new BmwOptions
        {
            ClientId = "test-client-id",
            DeviceFlowBaseUrl = _mockServer.BaseUrl,
            RefreshToken = "configured-refresh-token",
            InitialPollIntervalMs = 200,
            SlowDownIncrementMs = 500
        };

        var httpClient = new HttpClient();
        var optionsWrapper = Options.Create(optionsWithRefreshToken);
        var logger = NullLogger<AuthenticationService>.Instance;
        var authServiceWithRefresh = new AuthenticationService(httpClient, optionsWrapper, logger);

        _mockServer.SetupRefreshTokenSuccess(expectedAccessToken, expectedNewRefreshToken);

        // Act
        var result = await authServiceWithRefresh.GetAccessTokenAsync();

        // Assert
        Assert.That(result, Is.EqualTo(expectedAccessToken));
    }

    [Test]
    public void GetAccessTokenAsync_NoTokenAndNoRefreshToken_ThrowsInvalidOperationException()
    {
        // Arrange - service has no token and no configured refresh token

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.GetAccessTokenAsync());

        Assert.That(ex!.Message, Does.Contain("No valid access token available"));
        Assert.That(ex.Message, Does.Contain("InitiateDeviceFlowAsync"));
    }

    [Test]
    public async Task GetAccessTokenAsync_TokenNotExpired_ReturnsCachedToken()
    {
        // Arrange
        const string expectedAccessToken = "test-access-token";
        const string deviceCode = "device-123";

        // Complete device flow to get initial token
        _mockServer.SetupDeviceCodeSuccess(deviceCode, "USER-CODE");
        await _authService.InitiateDeviceFlowAsync("test-scope");

        _mockServer.Reset();
        _mockServer.SetupTokenSuccess(expectedAccessToken, "refresh-token");
        var deviceCodeResponse = CreateTestDeviceCodeResponse(deviceCode, expiresIn: 10);
        await _authService.PollForTokenAsync(deviceCodeResponse);

        // Reset mock to verify no additional calls are made
        _mockServer.Reset();

        // Act - call twice
        var result1 = await _authService.GetAccessTokenAsync();
        var result2 = await _authService.GetAccessTokenAsync();

        // Assert - both should return the same cached token
        Assert.That(result1, Is.EqualTo(expectedAccessToken));
        Assert.That(result2, Is.EqualTo(expectedAccessToken));
    }
}
