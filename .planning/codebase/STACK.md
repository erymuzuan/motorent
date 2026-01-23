# Technology Stack

**Analysis Date:** 2026-01-19

## Languages

**Primary:**
- C# 14 / .NET 10 - All application code (server, client, services, domain, workers)

**Secondary:**
- SQL - Database schema and queries (`database/*.sql`)
- JavaScript - Browser interop (`src/MotoRent.Client/wwwroot/*.js`)
- PowerShell - Build and environment scripts (`*.ps1`)

## Runtime

**Environment:**
- .NET 10 (net10.0 target framework)
- ASP.NET Core 10

**Package Manager:**
- NuGet
- Lockfile: Not detected (no `packages.lock.json`)

## Frameworks

**Core:**
- Blazor Server + WebAssembly Hybrid - Full-stack web framework
  - Server: `src/MotoRent.Server/MotoRent.Server.csproj` (Microsoft.NET.Sdk.Web)
  - Client: `src/MotoRent.Client/MotoRent.Client.csproj` (Microsoft.NET.Sdk.BlazorWebAssembly)
- ASP.NET Core 10 - Backend framework with MVC controllers

**Testing:**
- xUnit 2.9.2 - Unit testing framework
- Microsoft.NET.Test.Sdk 17.11.1 - Test platform
- coverlet.collector 6.0.2 - Code coverage

**Build/Dev:**
- MSBuild / dotnet CLI - Build tooling
- Visual Studio 2022 (v17+) - IDE

## Key Dependencies

**Critical:**
- `Microsoft.Data.SqlClient 5.2.2/6.0.1` - SQL Server connectivity
- `System.Text.Json` (built-in) - JSON serialization with polymorphism
- `Microsoft.AspNetCore.Components.WebAssembly 10.0.1` - Blazor WASM runtime
- `Microsoft.AspNetCore.Components.Authorization 10.0.1` - Auth state management

**Authentication:**
- `Microsoft.AspNetCore.Authentication.Google 10.0.1` - Google OAuth
- `Microsoft.AspNetCore.Authentication.MicrosoftAccount 10.0.1` - Microsoft OAuth
- `AspNet.Security.OAuth.Line 10.0.0` - LINE OAuth (Thailand-specific)
- `System.IdentityModel.Tokens.Jwt 8.15.0` - JWT token handling
- `Microsoft.IdentityModel.Tokens 8.15.0` - Token validation

**Infrastructure:**
- `AWSSDK.S3 4.0.17` - AWS S3 file storage
- `RabbitMQ.Client 7.2.0` - Message broker (optional)
- `Polly 8.6.5` - Resilience and retry policies

**Utilities:**
- `Hashids.net 1.7.0` - URL-safe ID encoding
- `Microsoft.Extensions.Localization 10.0.1` - Multi-language support
- `Spectre.Console 0.54.0` - Console formatting (workers)
- `CommandLineParser 2.9.1` - CLI argument parsing (workers)

**Real-Time:**
- `Microsoft.AspNetCore.SignalR.Client 10.0.0` - Real-time client (workers)
- ASP.NET Core SignalR (built-in) - Real-time server

## Configuration

**Environment:**
- Environment variables with `MOTO_` prefix
- Managed via `MotoConfig.cs` static class
- Template: `env.motorent.template.ps1`
- Override: `env.motorent.local.ps1` (gitignored)

**Key configs required:**
- `MOTO_SqlConnectionString` - Database connection
- `MOTO_GoogleClientId` / `MOTO_GoogleClientSecret` - Google OAuth
- `MOTO_MicrosoftClientId` / `MOTO_MicrosoftClientSecret` - Microsoft OAuth
- `MOTO_LineChannelId` / `MOTO_LineChannelSecret` - LINE OAuth
- `MOTO_GeminiApiKey` - OCR functionality
- `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` - S3 storage

**Build:**
- `MotoRent.sln` - Solution file
- `build.web.ps1` - Web build script
- `appsettings.json` / `appsettings.Development.template.json` - ASP.NET configuration

## Project Structure

| Project | Type | Purpose |
|---------|------|---------|
| `MotoRent.Server` | Blazor Server | Web host, authentication, API controllers |
| `MotoRent.Client` | Blazor WASM | PWA client components, interop |
| `MotoRent.Domain` | Class Library | Entities, data context, core abstractions |
| `MotoRent.Services` | Class Library | Business logic, external integrations |
| `MotoRent.Messaging` | Class Library | RabbitMQ message broker |
| `MotoRent.Worker` | Console App | Background message subscribers |
| `MotoRent.Scheduler` | Console App | Scheduled task runners |
| `MotoRent.Domain.Tests` | Test Project | Unit tests |

## Platform Requirements

**Development:**
- Windows (PowerShell scripts)
- .NET 10 SDK
- SQL Server (local instance `.\DEV2022` or Docker)
- Visual Studio 2022 or VS Code with C# extension

**Production:**
- Windows Server / Linux with .NET 10 runtime
- SQL Server 2019+
- AWS S3 buckets (public + private)
- Optional: RabbitMQ server
- Optional: OpenSearch for full-text search

---

*Stack analysis: 2026-01-19*
