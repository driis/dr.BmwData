using dr.BmwData.Models;
using dr.BmwData.Tests.Mocks;
using Microsoft.Extensions.Options;

namespace dr.BmwData.Tests;

[TestFixture]
public class ContainerServiceTests
{
    private BmwApiMockServer _mockServer = null!;
    private ContainerService _containerService = null!;
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

        _containerService = new ContainerService(httpClient, optionsWrapper, _mockAuthService);
    }

    [TearDown]
    public void TearDown()
    {
        _mockServer?.Dispose();
    }

    [Test]
    public async Task CreateContainerAsync_Success_ReturnsContainerResponse()
    {
        // Arrange
        const string expectedContainerId = "container-123";
        var technicalDescriptors = new[] { "FUEL_LEVEL", "MILEAGE", "CHARGING_STATUS" };

        _mockServer.SetupCreateContainerSuccess(expectedContainerId, technicalDescriptors);

        // Act
        var result = await _containerService.CreateContainerAsync(technicalDescriptors);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainerId, Is.EqualTo(expectedContainerId));
        Assert.That(result.Name, Is.EqualTo("CarData Container"));
        Assert.That(result.Purpose, Is.EqualTo("Telemetry data collection"));
        Assert.That(result.State, Is.EqualTo(ContainerState.ACTIVE));
        Assert.That(result.TechnicalDescriptors, Is.EqualTo(technicalDescriptors));
    }

    [Test]
    public async Task CreateContainerAsync_WithSingleDescriptor_ReturnsContainerResponse()
    {
        // Arrange
        const string expectedContainerId = "container-456";
        var technicalDescriptors = new[] { "MILEAGE" };

        _mockServer.SetupCreateContainerSuccess(expectedContainerId, technicalDescriptors);

        // Act
        var result = await _containerService.CreateContainerAsync(technicalDescriptors);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainerId, Is.EqualTo(expectedContainerId));
        Assert.That(result.TechnicalDescriptors, Has.Length.EqualTo(1));
        Assert.That(result.TechnicalDescriptors[0], Is.EqualTo("MILEAGE"));
    }

    [Test]
    public void CreateContainerAsync_Unauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        var technicalDescriptors = new[] { "FUEL_LEVEL" };

        _mockServer.SetupCreateContainerUnauthorized();

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _containerService.CreateContainerAsync(technicalDescriptors));
    }

    [Test]
    public void CreateContainerAsync_BadRequest_ThrowsHttpRequestException()
    {
        // Arrange
        var technicalDescriptors = new[] { "INVALID_DESCRIPTOR" };

        _mockServer.SetupCreateContainerBadRequest("Invalid technical descriptors");

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _containerService.CreateContainerAsync(technicalDescriptors));
    }

    [Test]
    public async Task ListContainersAsync_Success_ReturnsContainerList()
    {
        // Arrange
        _mockServer.SetupListContainersSuccess(
            ("container-1", "Container One", "Purpose One", "ACTIVE"),
            ("container-2", "Container Two", "Purpose Two", "ACTIVE"));

        // Act
        var result = await _containerService.ListContainersAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Containers, Has.Length.EqualTo(2));
        Assert.That(result.Containers[0].ContainerId, Is.EqualTo("container-1"));
        Assert.That(result.Containers[0].Name, Is.EqualTo("Container One"));
        Assert.That(result.Containers[1].ContainerId, Is.EqualTo("container-2"));
    }

    [Test]
    public async Task ListContainersAsync_Empty_ReturnsEmptyList()
    {
        // Arrange
        _mockServer.SetupListContainersEmpty();

        // Act
        var result = await _containerService.ListContainersAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Containers, Is.Empty);
    }

    [Test]
    public void ListContainersAsync_Unauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        _mockServer.SetupListContainersUnauthorized();

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _containerService.ListContainersAsync());
    }

    [Test]
    public async Task GetContainerAsync_Success_ReturnsContainerResponse()
    {
        // Arrange
        const string containerId = "container-123";
        var technicalDescriptors = new[] { "FUEL_LEVEL", "MILEAGE" };

        _mockServer.SetupGetContainerSuccess(containerId, technicalDescriptors);

        // Act
        var result = await _containerService.GetContainerAsync(containerId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainerId, Is.EqualTo(containerId));
        Assert.That(result.Name, Is.EqualTo("CarData Container"));
        Assert.That(result.State, Is.EqualTo(ContainerState.ACTIVE));
        Assert.That(result.TechnicalDescriptors, Is.EqualTo(technicalDescriptors));
    }

    [Test]
    public void GetContainerAsync_NotFound_ThrowsHttpRequestException()
    {
        // Arrange
        const string containerId = "non-existent-container";

        _mockServer.SetupGetContainerNotFound(containerId);

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _containerService.GetContainerAsync(containerId));
    }

    [Test]
    public async Task DeleteContainerAsync_Success_Completes()
    {
        // Arrange
        const string containerId = "container-123";

        _mockServer.SetupDeleteContainerSuccess(containerId);

        // Act & Assert - should complete without throwing
        await _containerService.DeleteContainerAsync(containerId);
    }

    [Test]
    public void DeleteContainerAsync_NotFound_ThrowsHttpRequestException()
    {
        // Arrange
        const string containerId = "non-existent-container";

        _mockServer.SetupDeleteContainerNotFound(containerId);

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _containerService.DeleteContainerAsync(containerId));
    }
}
