# dr.BmwData

[![.Build](https://github.com/driis/dr.BmwData/actions/workflows/dotnet.yml/badge.svg)](https://github.com/driis/dr.BmwData/actions/workflows/dotnet.yml)

A .NET library to access telemetry from [BMW Open Car Data](https://bmw-cardata.bmwgroup.com/customer/public/home). Very much work in progress. 

AI Agents must read the [ARCHITECTURE.md](docs/ARCHITECTURE.md) file before any task is started.
## Goals

- [ ] Demonstrate how to access BMW open car data
- [ ] Poll and Store data from my own vehicle, somewhere
- [ ] Possibly find ways of using this data in, for example, home assistant. Maybe as an MQTT sensor?

This project just started and is mainly scaffolding for now. It's my pet project and it's unlikely I will ever finish it into production quality. However if you find it useful, feel free to send PRs or open issues.

## Overview

This project consists of:
- **dr.BmwData**: A reusable class library containing the main implementation for accessing the BMW Open Car Data API
- **dr.BmwData.Console**: A console application demonstrating the usage of the library and outputting telemetry data

The library implements **OAuth 2.0 Device Code Flow** for authentication and uses modern C# patterns (records, dependency injection, options pattern).

## Quick Start

1. **Configure** your BMW API client ID in `appsettings.json`:
```json
{
  "BmwData": {
    "ClientId": "your-client-id-here"
  }
}
```

2. **Run** the console app:
```bash
cd src/dr.BmwData.Console
dotnet run
```

3. **Authorize** by visiting the displayed URL and entering the code

4. The app will retrieve and display your vehicle telemetry data

## Documentation

- **[Architecture Documentation](docs/ARCHITECTURE.md)** - Detailed technical information about authentication flow, configuration, design patterns, and API endpoints
- **[BMW Open Car Data Portal](https://bmw-cardata.bmwgroup.com/customer/public/home)** - Official BMW API documentation

## Development Notes

- Built with .NET 9.0
- Uses modern C# features (records, primary constructors, target-typed new)
- Follows clean architecture with separation of concerns
- Uses dependency injection and the Options pattern
