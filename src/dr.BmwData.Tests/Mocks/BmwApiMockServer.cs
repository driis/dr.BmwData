using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace dr.BmwData.Tests.Mocks;

/// <summary>
/// Encapsulates WireMock server configuration for BMW CarData API endpoints.
/// Provides methods to configure container and other API endpoint responses.
/// </summary>
public class BmwApiMockServer : IDisposable
{
    private readonly WireMockServer _server;

    public string BaseUrl => _server.Url!;

    public BmwApiMockServer()
    {
        _server = WireMockServer.Start();
    }

    /// <summary>
    /// Configures the create container endpoint to return a successful response.
    /// </summary>
    public void SetupCreateContainerSuccess(string containerId, string[] technicalDescriptors)
    {
        var descriptorsJson = System.Text.Json.JsonSerializer.Serialize(technicalDescriptors);

        _server
            .Given(Request.Create()
                .WithPath("/customers/containers")
                .WithHeader("Authorization", "Bearer *")
                .WithHeader("x-version", "v1")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Created)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""containerId"": ""{containerId}"",
                    ""name"": ""CarData Container"",
                    ""purpose"": ""Telemetry data collection"",
                    ""state"": ""ACTIVE"",
                    ""created"": ""2024-01-15T10:30:00Z"",
                    ""technicalDescriptors"": {descriptorsJson}
                }}"));
    }

    /// <summary>
    /// Configures the create container endpoint to return an unauthorized error.
    /// </summary>
    public void SetupCreateContainerUnauthorized()
    {
        _server
            .Given(Request.Create()
                .WithPath("/customers/containers")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""unauthorized"", ""message"": ""Invalid or expired access token""}"));
    }

    /// <summary>
    /// Configures the create container endpoint to return a bad request error.
    /// </summary>
    public void SetupCreateContainerBadRequest(string message = "Invalid technical descriptors")
    {
        _server
            .Given(Request.Create()
                .WithPath("/customers/containers")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""error"": ""bad_request"", ""message"": ""{message}""}}"));
    }

    /// <summary>
    /// Configures the list containers endpoint to return a successful response.
    /// </summary>
    public void SetupListContainersSuccess(params (string containerId, string name, string purpose, string state)[] containers)
    {
        var containersJson = string.Join(",", containers.Select(c => $@"{{
            ""containerId"": ""{c.containerId}"",
            ""name"": ""{c.name}"",
            ""purpose"": ""{c.purpose}"",
            ""state"": ""{c.state}"",
            ""created"": ""2024-01-15T10:30:00Z""
        }}"));

        _server
            .Given(Request.Create()
                .WithPath("/customers/containers")
                .WithHeader("Authorization", "Bearer *")
                .WithHeader("x-version", "v1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""containers"": [{containersJson}]}}"));
    }

    /// <summary>
    /// Configures the list containers endpoint to return an empty list.
    /// </summary>
    public void SetupListContainersEmpty()
    {
        _server
            .Given(Request.Create()
                .WithPath("/customers/containers")
                .WithHeader("Authorization", "Bearer *")
                .WithHeader("x-version", "v1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""containers"": []}"));
    }

    /// <summary>
    /// Configures the get container endpoint to return a successful response.
    /// </summary>
    public void SetupGetContainerSuccess(string containerId, string[] technicalDescriptors)
    {
        var descriptorsJson = System.Text.Json.JsonSerializer.Serialize(technicalDescriptors);

        _server
            .Given(Request.Create()
                .WithPath($"/customers/containers/{containerId}")
                .WithHeader("Authorization", "Bearer *")
                .WithHeader("x-version", "v1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""containerId"": ""{containerId}"",
                    ""name"": ""CarData Container"",
                    ""purpose"": ""Telemetry data collection"",
                    ""state"": ""ACTIVE"",
                    ""created"": ""2024-01-15T10:30:00Z"",
                    ""technicalDescriptors"": {descriptorsJson}
                }}"));
    }

    /// <summary>
    /// Configures the get container endpoint to return a not found error.
    /// </summary>
    public void SetupGetContainerNotFound(string containerId)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/customers/containers/{containerId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NotFound)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""not_found"", ""message"": ""Container not found""}"));
    }

    /// <summary>
    /// Configures the delete container endpoint to return a successful response.
    /// </summary>
    public void SetupDeleteContainerSuccess(string containerId)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/customers/containers/{containerId}")
                .WithHeader("Authorization", "Bearer *")
                .WithHeader("x-version", "v1")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    /// <summary>
    /// Configures the delete container endpoint to return a not found error.
    /// </summary>
    public void SetupDeleteContainerNotFound(string containerId)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/customers/containers/{containerId}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NotFound)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""not_found"", ""message"": ""Container not found""}"));
    }

    /// <summary>
    /// Configures the list containers endpoint to return an unauthorized error.
    /// </summary>
    public void SetupListContainersUnauthorized()
    {
        _server
            .Given(Request.Create()
                .WithPath("/customers/containers")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""unauthorized"", ""message"": ""Invalid or expired access token""}"));
    }

    /// <summary>
    /// Resets all configured mappings.
    /// </summary>
    public void Reset()
    {
        _server.Reset();
    }

    public void Dispose()
    {
        _server?.Stop();
        _server?.Dispose();
    }
}
