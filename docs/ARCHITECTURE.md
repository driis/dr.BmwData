# Architecture Documentation

This document provides detailed technical information about the dr.BmwData library implementation.

## Authentication

The library implements the **OAuth 2.0 Device Code Flow** for authentication with the BMW CarData API.

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
