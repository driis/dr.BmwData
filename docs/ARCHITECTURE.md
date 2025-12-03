# Architecture Documentation

This document provides detailed technical information about the dr.BmwData library implementation.
It exists both for humans, but in particular to guide AI agents on the codebase.

## Authentication

The library implements the **OAuth 2.0 Device Code Flow** for authentication with the BMW CarData API.

## Agent rules 
* Before any task is started, the agent must read the ARCHITECTURE.md file.
* The agent must always follow the rules in the ARCHITECTURE.md file.
* When a task is completed, the agent must update the ARCHITECTURE.md file to reflect the changes.
* When a task is completed, there must be zero build warnings or errors.
* All tests must pass.
* Do not make any other changes than what is required to complete the task.
* Prefer modern C# features and patterns.

### Components

#### IAuthenticationService / AuthenticationService
Handles authentication and token management for the BMW CarData API.

**Token Management:**
- Stores access token and refresh token internally after successful authentication
- Automatically refreshes expired tokens using the refresh token
- Persists refresh tokens via `IRefreshTokenStore` for reuse across application restarts

**Properties:**
- `RequiresInteractiveFlow` (obsolete): Synchronous check - use `RequiresInteractiveFlowAsync()` instead

**Methods:**
- `RequiresInteractiveFlowAsync()`: Checks if interactive device flow is needed
  - Returns `true` if no valid access token exists and no refresh token is available
  - Returns `false` if token can be obtained automatically (via cache, options, or store)
  - Loads refresh token from `IRefreshTokenStore` if configured

- `GetAccessTokenAsync()`: Gets a valid access token
  - Returns cached token if not expired
  - Loads refresh token from store if not already loaded
  - Uses refresh token to obtain new access token if expired
  - Saves new refresh token to store after refresh
  - Throws `InvalidOperationException` if no token and no refresh token available

- `InitiateDeviceFlowAsync(string scope)`: Starts the interactive authentication flow
  - Validates that `ClientId` is configured (throws `InvalidOperationException` if missing)
  - Creates a PKCE challenge
  - Sends device code request to BMW auth server
  - Returns `DeviceCodeResponse` with user code and verification URL

- `PollForTokenAsync(DeviceCodeResponse deviceCodeResponse)`: Polls for access token
  - Takes the `DeviceCodeResponse` from `InitiateDeviceFlowAsync`
  - Uses the interval and expiration from the response (respects server's polling interval)
  - Waits for user to authorize via browser
  - Handles `authorization_pending` and `slow_down` responses
  - Stores token internally and persists refresh token to store upon success

**Private Methods:**
- `RefreshTokenAsync(string refreshToken)`: Refreshes the access token internally
  - Called automatically by `GetAccessTokenAsync()` when token is expired
  - Saves new refresh token to store after successful refresh

#### IRefreshTokenStore (Required)
Abstraction for persisting and loading refresh tokens. Required dependency for `AuthenticationService`. Allows the application to save refresh tokens so they survive application restarts.

**Methods:**
- `LoadAsync()`: Loads the stored refresh token, returns null if none stored
- `SaveAsync(string refreshToken)`: Persists the refresh token

**Implementations:**
- `FileRefreshTokenStore`: File-based implementation
  - Default location: `~/.bmwdata/refresh_token`
  - Creates directory if it doesn't exist
  - Accepts custom file path via constructor

**Authentication Flow:**
1. First run: No stored token → Interactive device flow required → Token saved to store
2. Subsequent runs: Token loaded from store → Automatic refresh → New token saved back to store

#### IContainerService / ContainerService
Handles container management for the BMW CarData API. Uses `IAuthenticationService` internally to obtain access tokens.

**Methods:**
- `CreateContainerAsync(string[] technicalDescriptors)`: Creates a new container
  - Takes an array of technical descriptors (e.g., "FUEL_LEVEL", "MILEAGE", "CHARGING_STATUS")
  - Returns `ContainerResponse` with container ID, state, and creation timestamp

- `ListContainersAsync()`: Lists all containers
  - Returns `ContainerListResponse` with array of `ContainerSummary` objects

- `GetContainerAsync(string containerId)`: Gets container details
  - Returns `ContainerResponse` with full container details including technical descriptors

- `DeleteContainerAsync(string containerId)`: Deletes a container
  - Returns void on success (HTTP 204 No Content)

#### CodeChallenge
Generates PKCE (Proof Key for Code Exchange) values for secure authentication.

**Properties:**
- `Challenge`: SHA256 hash of the verifier (sent to server)
- `Verification`: Random code verifier (sent during token exchange)

**Implementation:**
- Uses `RandomNumberGenerator` for cryptographically secure random bytes
- Generates 32-byte verifier
- Creates SHA256 hash for challenge
- Encodes using Base64 URL-safe encoding

#### Authentication Models

All models use **C# records with primary constructors**:

- `DeviceCodeRequest`: Request to initiate device flow
  - `ClientId`, `Scope`, `CodeChallenge`
  - Constants: `ResponseType = "device_code"`, `CodeChallengeMethod = "S256"`

- `DeviceCodeResponse`: Response from device flow initiation
  - `DeviceCode`, `UserCode`, `VerificationUri`, `VerificationUriComplete`
  - `ExpiresIn`, `Interval`

- `TokenRequest`: Request to exchange device code for token
  - `ClientId`, `DeviceCode`, `CodeVerifier`
  - Constant: `GrantType = "urn:ietf:params:oauth:grant-type:device_code"`

- `TokenResponse`: Access token response
  - `AccessToken`, `TokenType`, `ExpiresIn`, `RefreshToken`, `Scope`

#### Container Models

- `CreateContainerRequest`: Request to create a new container
  - `Name`, `Purpose`, `TechnicalDescriptors`

- `ContainerResponse`: Response from container creation/get details
  - `ContainerId`, `Name`, `Purpose`, `State`, `Created`, `TechnicalDescriptors`

- `ContainerListResponse`: Response from list containers
  - `Containers` (array of `ContainerSummary`)

- `ContainerSummary`: Summary of a container (without technical descriptors)
  - `ContainerId`, `Name`, `Purpose`, `State`, `Created`

- `ContainerState`: Enum representing container state
  - `ACTIVE`, `DELETED`

### Authentication Flow

1. **Initiate**: Call `InitiateDeviceFlowAsync()`
   - Library generates PKCE challenge
   - Sends request to `{DeviceFlowBaseUrl}/gcdm/oauth/device/code`
   - Returns user code and verification URL

2. **User Authorization**: User visits URL and enters code
   - User authenticates with BMW credentials
   - Authorizes the application

3. **Poll for Token**: Call `PollForTokenAsync()`
   - Library polls `{DeviceFlowBaseUrl}/gcdm/oauth/token`
   - Waits for user to complete authorization
   - Returns access token when ready

4. **Use Token**: Access BMW API with the token
   - Include token in API requests
   - Token expires after `ExpiresIn` seconds
   - Use refresh token to get new access token

## Configuration

The library uses the **Options Pattern** for configuration management.

### BmwOptions

Configuration class that holds all BMW API settings.

**Properties:**

- `ClientId` (required): Your BMW API client ID
  - Obtained from BMW Developer Portal
  - Used for authentication requests

- `DeviceFlowBaseUrl`: BMW authentication server URL
  - Default: `https://customer.bmwgroup.com`
  - Used for OAuth device flow endpoints

- `BaseUrl`: BMW API base URL
  - Default: `https://bmw-cardata.bmwgroup.com/thirdparty/public/home`
  - Used for API data requests

- `ApiBaseUrl`: BMW CarData API base URL
  - Default: `https://api-cardata.bmwgroup.com`
  - Used for container and telemetry API endpoints

- `InitialPollIntervalMs`: Initial polling interval in milliseconds
  - Default: `1000` (1 second)
  - Controls how often to poll for token during device flow
  - Can be reduced for faster tests

- `SlowDownIncrementMs`: Interval increment when slow_down is received
  - Default: `5000` (5 seconds)
  - Added to current interval when server requests slower polling
  - Can be reduced for faster tests

### Configuration Sources

The application supports multiple configuration sources (in order of precedence):

1. **Environment Variables**: Prefix with `BmwData__`
   ```bash
   BmwData__ClientId=your-client-id
   ```

2. **appsettings.json**:
   ```json
   {
     "BmwData": {
       "ClientId": "your-client-id-here",
       "DeviceFlowBaseUrl": "https://customer.bmwgroup.com",
       "BaseUrl": "https://bmw-cardata.bmwgroup.com/thirdparty/public/home"
     }
   }
   ```

3. **.env file** (if using dotenv package)

### Dependency Injection Setup

```csharp
builder.Services.Configure<BmwOptions>(
    builder.Configuration.GetSection(BmwOptions.SectionName));

// Register refresh token store for persistent token storage
builder.Services.AddSingleton<IRefreshTokenStore, FileRefreshTokenStore>();

builder.Services.AddHttpClient<AuthenticationService>();
builder.Services.AddSingleton<IAuthenticationService>(sp => sp.GetRequiredService<AuthenticationService>());
builder.Services.AddHttpClient<IContainerService, ContainerService>();
```

## Design Patterns

### Options Pattern
- Strongly-typed configuration
- Supports configuration reloading
- Validates configuration at startup

### Dependency Injection
- Services registered in DI container
- HttpClient factory pattern for `AuthenticationService`
- Scoped/singleton lifetimes as appropriate

### Records with Primary Constructors
- Immutable data models
- Concise syntax
- Value-based equality
- Example: `public record TokenResponse(string AccessToken, ...)`

### Helper Methods
- `ToFormUrlEncodedContent<T>()`: Converts models to form-encoded content
  - Serializes to JSON
  - Deserializes to Dictionary
  - Creates FormUrlEncodedContent for HTTP requests

## API Endpoints

### Device Flow Endpoints

**Initiate Device Flow:**
- URL: `POST {DeviceFlowBaseUrl}/gcdm/oauth/device/code`
- Content-Type: `application/x-www-form-urlencoded`
- Request: `DeviceCodeRequest`
- Response: `DeviceCodeResponse`

**Token Exchange:**
- URL: `POST {DeviceFlowBaseUrl}/gcdm/oauth/token`
- Content-Type: `application/x-www-form-urlencoded`
- Request: `TokenRequest`
- Response: `TokenResponse` or error

### Container Endpoints

All container endpoints require headers: `Authorization: Bearer {accessToken}`, `x-version: v1`

**Create Container:**
- URL: `POST {ApiBaseUrl}/customers/containers`
- Content-Type: `application/json`
- Request: `CreateContainerRequest`
- Response: `ContainerResponse` (HTTP 201)

**List Containers:**
- URL: `GET {ApiBaseUrl}/customers/containers`
- Response: `ContainerListResponse` (HTTP 200)

**Get Container:**
- URL: `GET {ApiBaseUrl}/customers/containers/{containerId}`
- Response: `ContainerResponse` (HTTP 200)

**Delete Container:**
- URL: `DELETE {ApiBaseUrl}/customers/containers/{containerId}`
- Response: No content (HTTP 204)

### Error Handling

The authentication service handles these error scenarios:

- `authorization_pending`: User hasn't authorized yet (continues polling)
- `slow_down`: Polling too fast (increases interval by 5 seconds)
- Timeout: Device code expires (throws `TimeoutException`)
- Other errors: Throws `Exception` with error details

## Testing

The library includes comprehensive unit tests using **NUnit** and **WireMock.Net** for HTTP-level mocking.

### Test Infrastructure

**Test Project:** `dr.BmwData.Tests`
- Framework: NUnit 3
- HTTP Mocking: WireMock.Net
- Target: .NET 9.0
- Uses fast polling intervals (200ms/500ms) for quick test execution

### BmwAuthMockServer

Encapsulated mock server configuration type for BMW authentication endpoints.

**Purpose:**
- Wraps WireMock.Net server lifecycle
- Provides type-safe methods to configure endpoint responses
- Ensures all BMW authentication mocking logic is centralized and reusable

**Key Methods:**
- `SetupDeviceCodeSuccess()`: Configures device code endpoint with successful response
- `SetupTokenSuccess()`: Configures token endpoint with successful token response
- `SetupTokenAuthorizationPending()`: Returns authorization_pending error
- `SetupTokenSlowDown()`: Returns slow_down error
- `SetupTokenError()`: Returns custom error responses
- `SetupTokenPendingThenSuccess()`: Simulates polling scenario (pending N times, then success)

**Design Benefits:**
- Tests have no direct dependency on BMW services
- HTTP-level mocking ensures realistic integration testing
- Encapsulation makes tests more maintainable
- Reusable across multiple test scenarios

### BmwApiMockServer

Encapsulated mock server configuration type for BMW CarData API endpoints.

**Purpose:**
- Wraps WireMock.Net server lifecycle for API endpoints
- Provides type-safe methods to configure container endpoint responses
- Ensures all BMW API mocking logic is centralized and reusable

**Key Methods:**
- `SetupCreateContainerSuccess()`: Configures container creation endpoint with successful response
- `SetupCreateContainerUnauthorized()`: Returns 401 unauthorized error
- `SetupCreateContainerBadRequest()`: Returns 400 bad request error
- `SetupListContainersSuccess()`: Configures list containers endpoint with successful response
- `SetupListContainersEmpty()`: Returns empty container list
- `SetupListContainersUnauthorized()`: Returns 401 unauthorized error
- `SetupGetContainerSuccess()`: Configures get container endpoint with successful response
- `SetupGetContainerNotFound()`: Returns 404 not found error
- `SetupDeleteContainerSuccess()`: Configures delete container endpoint with 204 response
- `SetupDeleteContainerNotFound()`: Returns 404 not found error

### Running Tests

Run all tests using the .NET CLI:

```bash
dotnet test dr.BmwData.sln --verbosity normal
```

Or run tests for the test project only:

```bash
dotnet test src/dr.BmwData.Tests/dr.BmwData.Tests.csproj
```

### Test Coverage

**AuthenticationService Tests:**

1. `InitiateDeviceFlowAsync_Success_ReturnsDeviceCodeResponse`
   - Verifies successful device code initiation
   - Validates response contains correct device code, user code, and URLs

2. `PollForTokenAsync_Success_ReturnsTokenResponse`
   - Verifies successful token retrieval
   - Validates access token and refresh token are returned

3. `PollForTokenAsync_AuthorizationPending_ThenSuccess_ReturnsToken`
   - Tests polling behavior when authorization is pending
   - Verifies service continues polling until success

4. `PollForTokenAsync_SlowDown_IncreasesInterval`
   - Tests slow_down error handling
   - Verifies polling interval increases by 5 seconds

5. `PollForTokenAsync_Timeout_ThrowsTimeoutException`
   - Tests timeout scenario when device code expires
   - Verifies `TimeoutException` is thrown

6. `PollForTokenAsync_Error_ThrowsException`
   - Tests error handling for unexpected errors
   - Verifies exception contains error details

7. `PollForTokenAsync_WithoutInitiate_ThrowsInvalidOperationException`
   - Tests that polling without initiation fails
   - Verifies `InvalidOperationException` is thrown

8. `RequiresInteractiveFlow_NoTokenAndNoRefreshToken_ReturnsTrue`
   - Tests that interactive flow is required when no tokens available

9. `RequiresInteractiveFlow_AfterDeviceFlow_ReturnsFalse`
    - Tests that interactive flow is not required after successful device flow

10. `GetAccessTokenAsync_AfterDeviceFlow_ReturnsStoredToken`
    - Verifies token is stored and returned after device flow

11. `GetAccessTokenAsync_NoTokenAndNoRefreshToken_ThrowsInvalidOperationException`
    - Tests error when no token available

12. `GetAccessTokenAsync_TokenNotExpired_ReturnsCachedToken`
    - Verifies cached token is returned without refreshing

13. `InitiateDeviceFlowAsync_MissingClientId_ThrowsInvalidOperationException`
    - Tests that empty ClientId throws exception with helpful message

14. `InitiateDeviceFlowAsync_NullClientId_ThrowsInvalidOperationException`
    - Tests that null ClientId throws exception

15. `RequiresInteractiveFlowAsync_WithStoredRefreshToken_ReturnsFalse`
    - Tests that stored refresh token from IRefreshTokenStore is loaded

16. `RequiresInteractiveFlowAsync_NoTokenAnywhere_ReturnsTrue`
    - Tests that interactive flow is required when no tokens available anywhere

17. `GetAccessTokenAsync_WithStoredRefreshToken_RefreshesAndReturnsToken`
    - Tests automatic token refresh using stored refresh token

18. `PollForTokenAsync_Success_SavesRefreshTokenToStore`
    - Verifies refresh token is persisted to store after device flow

19. `RefreshToken_UpdatesStoredToken`
    - Verifies new refresh token is saved after token refresh

**ContainerService Tests:**

1. `CreateContainerAsync_Success_ReturnsContainerResponse`
   - Verifies successful container creation
   - Validates response contains correct container ID, name, purpose, and state

2. `CreateContainerAsync_WithSingleDescriptor_ReturnsContainerResponse`
   - Tests container creation with a single technical descriptor

3. `CreateContainerAsync_Unauthorized_ThrowsHttpRequestException`
   - Tests 401 unauthorized error handling

4. `CreateContainerAsync_BadRequest_ThrowsHttpRequestException`
   - Tests 400 bad request error handling for invalid descriptors

5. `ListContainersAsync_Success_ReturnsContainerList`
   - Verifies successful container listing with multiple containers

6. `ListContainersAsync_Empty_ReturnsEmptyList`
   - Tests empty container list response

7. `ListContainersAsync_Unauthorized_ThrowsHttpRequestException`
   - Tests 401 unauthorized error handling for list operation

8. `GetContainerAsync_Success_ReturnsContainerResponse`
   - Verifies successful container retrieval with full details

9. `GetContainerAsync_NotFound_ThrowsHttpRequestException`
   - Tests 404 not found error handling

10. `DeleteContainerAsync_Success_Completes`
    - Verifies successful container deletion (no exception thrown)

11. `DeleteContainerAsync_NotFound_ThrowsHttpRequestException`
    - Tests 404 not found error handling for delete operation

All tests use mock servers (`BmwAuthMockServer` and `BmwApiMockServer`) to mock HTTP endpoints, ensuring tests are fast, reliable, and independent of external services.

## Console Application

The solution includes a command-line console application for interacting with the BMW CarData API.

### Project: dr.BmwData.Console

A CLI tool for container management operations.

### Command-Line Interface

**Usage:**
```bash
dr.BmwData.Console <command> [arguments]
```

**Commands:**
- `help` (or `-h`, `--help`, `-?`): Show help message
- `list`: List all containers
- `create <descriptor> [...]`: Create a container with specified technical descriptors
- `get <containerId>`: Get container details (outputs JSON)
- `delete <containerId>`: Delete a container

**Examples:**
```bash
dr.BmwData.Console list
dr.BmwData.Console create FUEL_LEVEL MILEAGE CHARGING_STATUS
dr.BmwData.Console get abc123-container-id
dr.BmwData.Console delete abc123-container-id
```

**Notes:**
- Commands are case-insensitive
- If no arguments provided, help is displayed
- Invalid commands show error message and help

### Components

#### CommandLineArgs
Record type for parsed command-line arguments.

**Properties:**
- `Command`: The command to execute (enum: Help, List, Create, Get, Delete)
- `ContainerId`: Container ID for get/delete commands
- `TechnicalDescriptors`: Array of descriptors for create command

**Static Methods:**
- `Parse(string[] args)`: Parses command-line arguments
  - Throws `ArgumentException` for invalid commands or missing arguments
- `PrintHelp()`: Displays usage information

#### BmwConsoleApp
Main application class that orchestrates command execution.

**Constructor Dependencies:**
- `IAuthenticationService`: For authentication flow
- `IContainerService`: For container operations
- `ILogger<BmwConsoleApp>`: For logging

**Methods:**
- `RunAsync(CommandLineArgs args, CancellationToken ct)`: Main entry point
  - Handles help command without authentication
  - Ensures authentication for other commands
  - Delegates to command-specific methods

**Authentication Flow:**
- Checks `RequiresInteractiveFlow` property
- If true, initiates device code flow and displays verification URL/code
- User authenticates via browser
- Polls for token completion

### Exit Codes
- `0`: Success
- `1`: Error (invalid arguments, authentication failure, API error)
