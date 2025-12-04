using dr.BmwData.Models;
using dr.BmwData.Tests.Mocks;
using Microsoft.Extensions.Options;

namespace dr.BmwData.Tests;

[TestFixture]
public class TelemetryServiceTests
{
    private BmwApiMockServer _mockServer = null!;
    private TelemetryService _telemetryService = null!;
    private BmwOptions _options = null!;
    private MockAuthenticationService _mockAuthService = null!;

    [SetUp]
    public void Setup()
    {
        _mockServer = new BmwApiMockServer();
        _mockAuthService = new MockAuthenticationService("valid-access-token");

        _options = new BmwOptions
        {
            ClientId = "test-client-id",
            ApiBaseUrl = _mockServer.BaseUrl
        };

        var httpClient = new HttpClient();
        var optionsWrapper = Options.Create(_options);

        _telemetryService = new TelemetryService(httpClient, optionsWrapper, _mockAuthService);
    }

    [TearDown]
    public void TearDown()
    {
        _mockServer?.Dispose();
    }

    [Test]
    public async Task GetVehicleMappingsAsync_Success_ReturnsMappings()
    {
        // Arrange
        _mockServer.SetupGetVehicleMappingsSuccess(
            ("WBA12345678901234", "2024-01-15T10:30:00Z", "PRIMARY"),
            ("WBA98765432109876", "2024-02-20T14:45:00Z", "SECONDARY"));

        // Act
        var result = await _telemetryService.GetVehicleMappingsAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Mappings, Has.Length.EqualTo(2));
        Assert.That(result.Mappings[0].Vin, Is.EqualTo("WBA12345678901234"));
        Assert.That(result.Mappings[0].MappingType, Is.EqualTo(MappingType.PRIMARY));
        Assert.That(result.Mappings[1].Vin, Is.EqualTo("WBA98765432109876"));
        Assert.That(result.Mappings[1].MappingType, Is.EqualTo(MappingType.SECONDARY));
    }

    [Test]
    public async Task GetVehicleMappingsAsync_Empty_ReturnsEmptyList()
    {
        // Arrange
        _mockServer.SetupGetVehicleMappingsEmpty();

        // Act
        var result = await _telemetryService.GetVehicleMappingsAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Mappings, Is.Empty);
    }

    [Test]
    public void GetVehicleMappingsAsync_Unauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        _mockServer.SetupGetVehicleMappingsUnauthorized();

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _telemetryService.GetVehicleMappingsAsync());
    }

    [Test]
    public async Task GetTelematicDataAsync_Success_ReturnsTelematicData()
    {
        // Arrange
        const string vin = "WBA12345678901234";
        const string containerId = "container-123";
        var telematicData = new Dictionary<string, (string value, string unit, string timestamp)>
        {
            ["FUEL_LEVEL"] = ("75.5", "PERCENT", "2024-01-15T10:30:00Z"),
            ["MILEAGE"] = ("45230", "KILOMETERS", "2024-01-15T10:30:00Z")
        };

        _mockServer.SetupGetTelematicDataSuccess(vin, containerId, telematicData);

        // Act
        var result = await _telemetryService.GetTelematicDataAsync(vin, containerId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TelematicData, Has.Count.EqualTo(2));
        Assert.That(result.TelematicData["FUEL_LEVEL"].Value, Is.EqualTo("75.5"));
        Assert.That(result.TelematicData["FUEL_LEVEL"].Unit, Is.EqualTo("PERCENT"));
        Assert.That(result.TelematicData["MILEAGE"].Value, Is.EqualTo("45230"));
        Assert.That(result.TelematicData["MILEAGE"].Unit, Is.EqualTo("KILOMETERS"));
    }

    [Test]
    public void GetTelematicDataAsync_NotFound_ThrowsHttpRequestException()
    {
        // Arrange
        const string vin = "WBA00000000000000";
        const string containerId = "container-123";

        _mockServer.SetupGetTelematicDataNotFound(vin);

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _telemetryService.GetTelematicDataAsync(vin, containerId));
    }

    [Test]
    public void GetTelematicDataAsync_Unauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        const string vin = "WBA12345678901234";
        const string containerId = "container-123";

        _mockServer.SetupGetTelematicDataUnauthorized(vin);

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _telemetryService.GetTelematicDataAsync(vin, containerId));
    }

    [Test]
    public async Task GetTelematicDataAsync_NullTimestamp_DeserializesSuccessfully()
    {
        // Arrange
        const string vin = "WBA12345678901234";
        const string containerId = "container-123";

        _mockServer.SetupGetTelematicDataWithNullTimestamp(vin, containerId);

        // Act
        var result = await _telemetryService.GetTelematicDataAsync(vin, containerId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TelematicData["FUEL_LEVEL"].Timestamp, Is.Null);
    }
}
