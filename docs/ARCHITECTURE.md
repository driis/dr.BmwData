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
Handles the device code flow authentication process.

**Methods:**
- `InitiateDeviceFlowAsync(string scope)`: Starts the authentication flow
  - Creates a PKCE challenge
  - Sends device code request to BMW auth server
  - Returns `DeviceCodeResponse` with user code and verification URL
  
- `PollForTokenAsync(string clientId, string deviceCode, int interval, int expiresIn)`: Polls for access token
  - Waits for user to authorize via browser
  - Handles `authorization_pending` and `slow_down` responses
  - Returns `TokenResponse` with access token and refresh token

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

- `RefreshToken`: Optional refresh token
  - Can be stored to avoid re-authentication
  - Used to obtain new access tokens

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
   BmwData__RefreshToken=your-refresh-token
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

builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>();
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

All tests use the `BmwAuthMockServer` to mock HTTP endpoints, ensuring tests are fast, reliable, and independent of external services.
