# Technology Stack

**Analysis Date:** 2026-01-23

## Languages

**Primary:**
- C# 14 (latest) - All backend services, domain, and Blazor components
- SQL - Database schema, stored procedures, JSON queries

**Secondary:**
- JavaScript - Browser interop (`src/MotoRent.Client/Interops/`)
- CSS - Component styling, Bootstrap/Tabler themes
- HTML - Razor component markup

## Runtime

**Environment:**
- .NET 10.0 - All projects target `net10.0`
- Blazor Server + WebAssembly (PWA hybrid)

**Package Manager:**
- NuGet
- Lockfile: Not detected (no `packages.lock.json`)

## Frameworks

**Core:**
- ASP.NET Core 10 - Web host, authentication, middleware
- Blazor Server - Primary rendering mode (`src/MotoRent.Server/`)
- Blazor WebAssembly - PWA client (`src/MotoRent.Client/`)

**Testing:**
- xUnit 2.9.2 - Test framework
- Microsoft.NET.Test.Sdk 17.11.1 - Test runner
- coverlet.collector 6.0.2 - Code coverage

**Build/Dev:**
- PowerShell - Build scripts (`build.web.ps1`, `StartMotoRentWeb.ps1`)
- Visual Studio / VS Code - IDE configuration present

## Key Dependencies

**Critical:**
- Microsoft.AspNetCore.Authentication.Google 10.0.1 - OAuth provider
- Microsoft.AspNetCore.Authentication.MicrosoftAccount 10.0.1 - OAuth provider
- AspNet.Security.OAuth.Line 10.0.0 - LINE OAuth for Thai market
- Microsoft.Data.SqlClient 6.0.1 - SQL Server connectivity
- System.Text.Json - JSON serialization with polymorphism
- System.IdentityModel.Tokens.Jwt 8.15.0 - JWT handling

**Infrastructure:**
- AWSSDK.S3 4.0.17 - File storage on AWS S3
- RabbitMQ.Client 7.2.0 - Message broker (optional)
- Microsoft.Extensions.Caching.Hybrid 9.3.0 - Distributed caching
- Polly 8.6.5 - Resilience and retry policies
- Microsoft.AspNetCore.SignalR.Client 10.0.0 - Real-time communications

**UI:**
- Microsoft.Extensions.Localization 10.0.1 - Multi-language support (en, th, ms)
- Hashids.net 1.7.0 - URL-safe ID encoding

**CLI/Worker:**
- CommandLineParser 2.9.1 - Worker CLI arguments
- Spectre.Console 0.54.0 - Rich console output

## Configuration

**Environment:**
- Environment variables with `MOTO_` prefix (see `MotoConfig.cs`)
- Configuration manager: `src/MotoRent.Domain/Core/MotoConfig.cs`
- Template: `env.motorent.template.ps1`

**Key Environment Variables:**
| Variable | Purpose |
|----------|---------|
| `MOTO_SqlConnectionString` | SQL Server connection |
| `MOTO_GoogleClientId/Secret` | Google OAuth |
| `MOTO_MicrosoftClientId/Secret` | Microsoft OAuth |
| `MOTO_LineChannelId/Secret` | LINE OAuth |
| `MOTO_JwtSecret` | JWT signing key |
| `MOTO_GeminiApiKey` | Google Gemini OCR |
| `MOTO_SuperAdmin` | Comma-separated admin emails |
| `AWS_ACCESS_KEY_ID` | S3 credentials (no prefix) |
| `AWS_SECRET_ACCESS_KEY` | S3 credentials (no prefix) |
| `AWS_REGION` | S3 region (default: ap-southeast-1) |

**Build:**
- `MotoRent.sln` - Solution file (8 projects + 1 test project)
- `src/MotoRent.Server/appsettings.json` - Runtime configuration
- `src/MotoRent.Server/Properties/launchSettings.json` - Development launch settings

## Project Structure

| Project | Type | Purpose |
|---------|------|---------|
| `MotoRent.Server` | Web (Blazor Server) | Main web host, authentication |
| `MotoRent.Client` | Blazor WASM | PWA client, UI components |
| `MotoRent.Domain` | Class Library | Entities, DataContext, interfaces |
| `MotoRent.Services` | Class Library | Business logic services |
| `MotoRent.Core.Repository` | Class Library | LINQ query translation, caching |
| `MotoRent.Messaging` | Class Library | RabbitMQ message broker |
| `MotoRent.Worker` | Console App | Background message processor |
| `MotoRent.Scheduler` | Console App | Scheduled tasks runner |
| `MotoRent.Domain.Tests` | Test | Unit tests for Domain |

## Platform Requirements

**Development:**
- .NET 10 SDK
- SQL Server (local or remote)
- Visual Studio 2022 / VS Code with C# extension
- PowerShell (for build scripts)

**Production:**
- Windows Server or Linux with .NET 10 runtime
- SQL Server (Azure SQL supported)
- AWS S3 access (for file storage)
- RabbitMQ (optional, for async messaging)
- OpenSearch (optional, for full-text search)

**Supported Cultures:**
- English (en) - Default
- Thai (th) - Primary market
- Bahasa Melayu (ms) - Secondary market

---

*Stack analysis: 2026-01-23*
