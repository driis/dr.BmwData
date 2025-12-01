# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Before Starting Any Task

**Read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) first.** This is mandatory and contains detailed technical documentation about authentication, configuration, and design patterns.

## Build and Test Commands

```bash
# Build the solution
dotnet build dr.BmwData.sln

# Run all tests
dotnet test dr.BmwData.sln

# Run a single test by name
dotnet test dr.BmwData.sln --filter "FullyQualifiedName~TestMethodName"

# Run the console app
dotnet run --project src/dr.BmwData.Console/dr.BmwData.Console.csproj
```

## Agent Rules

- When a task is completed, update ARCHITECTURE.md to reflect the changes
- Zero build warnings or errors allowed on completion
- All tests must pass
- Only make changes required to complete the task
- Prefer modern C# features and patterns (records, primary constructors, target-typed new)

## Project Structure

- **dr.BmwData** - Core library with BMW Open Car Data API client and OAuth 2.0 Device Code Flow authentication
- **dr.BmwData.Console** - Demo console app showing library usage
- **dr.BmwData.Tests** - NUnit tests using WireMock.Net for HTTP-level mocking

## Key Implementation Details

- Uses PKCE (Proof Key for Code Exchange) for secure authentication
- Configuration via Options Pattern (`BmwOptions`) - supports environment variables (`BmwData__*`) and appsettings.json
- All models use C# records with primary constructors
- Tests use `BmwAuthMockServer` wrapper for consistent BMW endpoint mocking with fast polling intervals (200ms/500ms)
