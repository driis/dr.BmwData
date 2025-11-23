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

    [SetUp]
    public void Setup()
    {
        _mockServer = new BmwAuthMockServer();
        
        _options = new BmwOptions
        {
            ClientId = "test-client-id",
            DeviceFlowBaseUrl = _mockServer.BaseUrl,
            BaseUrl = "https://mock.bmw.com",
            InitialPollIntervalMs = 200,  // Fast polling for tests
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
    public async Task PollForTokenAsync_Success_ReturnsTokenResponse()
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
        var result = await _authService.PollForTokenAsync(_options.ClientId, deviceCode, interval: 1, expiresIn: 10);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo(expectedAccessToken));
        Assert.That(result.RefreshToken, Is.EqualTo(expectedRefreshToken));
        Assert.That(result.TokenType, Is.EqualTo("Bearer"));
    }

    [Test]
    public async Task PollForTokenAsync_AuthorizationPending_ThenSuccess_ReturnsToken()
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
        var result = await _authService.PollForTokenAsync(_options.ClientId, deviceCode, interval: 1, expiresIn: 30);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo(expectedAccessToken));
        Assert.That(result.RefreshToken, Is.EqualTo(expectedRefreshToken));
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

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _authService.PollForTokenAsync(_options.ClientId, deviceCode, interval: 1, expiresIn: 30);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo(expectedAccessToken));
        Assert.That(result.RefreshToken, Is.EqualTo(expectedRefreshToken));
        // Verify that the delay increased (200ms initial + 700ms after slow_down = 900ms total)
        Assert.That(elapsed.TotalMilliseconds, Is.GreaterThan(600));
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

        // Act & Assert
        var exception = Assert.ThrowsAsync<TimeoutException>(async () =>
            await _authService.PollForTokenAsync(_options.ClientId, deviceCode, interval: 1, expiresIn: 3));
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
        var ex = Assert.ThrowsAsync<Exception>(async () =>
            await _authService.PollForTokenAsync(_options.ClientId, deviceCode, interval: 1, expiresIn: 10));

        Assert.That(ex.Message, Does.Contain("Token polling failed"));
        Assert.That(ex.Message, Does.Contain(expectedError));
    }

    [Test]
    public void PollForTokenAsync_WithoutInitiate_ThrowsInvalidOperationException()
    {
        // Arrange
        const string deviceCode = "device-123";

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.PollForTokenAsync(_options.ClientId, deviceCode, interval: 1, expiresIn: 10));
    }
}
