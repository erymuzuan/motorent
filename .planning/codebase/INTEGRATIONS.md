# External Integrations

**Analysis Date:** 2026-01-23

## APIs & External Services

**AI/ML:**
- Google Gemini API - Document OCR (passport, ID, driving license extraction)
  - SDK/Client: HttpClient via `IHttpClientFactory`
  - Auth: `MOTO_GeminiApiKey` env var
  - Model: `MOTO_GeminiModel` (default: `gemini-3-flash-preview`)
  - Implementation: `src/MotoRent.Services/DocumentOcrService.cs`
  - Endpoint: `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent`

**Mapping:**
- Google Maps API - Location picking, GPS coordinates
  - Auth: `MOTO_GoogleMapKey` env var
  - Implementation: `src/MotoRent.Client/Interops/GoogleMapJsInterop.cs`

## Data Storage

**Databases:**
- Microsoft SQL Server
  - Connection: `MOTO_SqlConnectionString` env var
  - Client: Microsoft.Data.SqlClient (custom repository pattern)
  - Schema: Multi-tenant with `[Core]` schema + tenant-specific `[AccountNo]` schemas
  - Pattern: JSON columns with computed columns for indexing
  - Schema files: `database/*.sql`

**File Storage:**
- AWS S3 - Binary file storage (images, documents)
  - Connection: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`
  - Private bucket: `MOTO_AwsBucket` (default: `motorent.private`)
  - Public bucket: `MOTO_AwsPublicBucket` (default: `motorent.public`)
  - Implementation: `src/MotoRent.Services/Storage/S3BinaryStore.cs`
  - Features: Pre-signed URLs, public/private access, metadata storage

**Caching:**
- Microsoft.Extensions.Caching.Hybrid - In-memory + distributed caching
  - Configuration: `builder.Services.AddHybridCache()`
  - Used by: Core Repository for query result caching

**Search (Optional):**
- OpenSearch - Full-text search engine
  - Connection: `MOTO_OpenSearchHost` env var (optional)
  - Auth: `MOTO_OpenSearchBasicAuth` env var
  - Implementation: `src/MotoRent.Services/Search/OpenSearchService.cs`
  - Used for: Entity search, fuzzy matching, code item lookup

## Authentication & Identity

**Auth Providers:**
- Google OAuth 2.0
  - Config: `MOTO_GoogleClientId`, `MOTO_GoogleClientSecret`
  - Callback: `/signin-google`
  - Implementation: `Microsoft.AspNetCore.Authentication.Google`

- Microsoft Account OAuth 2.0
  - Config: `MOTO_MicrosoftClientId`, `MOTO_MicrosoftClientSecret`
  - Callback: `/signin-microsoft`
  - Implementation: `Microsoft.AspNetCore.Authentication.MicrosoftAccount`

- LINE OAuth 2.0 (Thai market)
  - Config: `MOTO_LineChannelId`, `MOTO_LineChannelSecret`
  - Callback: `/signin-line`
  - Scopes: `profile`, `openid`
  - Implementation: `AspNet.Security.OAuth.Line`

**Session Management:**
- Cookie Authentication - 14-day sliding expiration
  - Cookie name: `MotoRent.Auth`
  - External auth cookie: `MotoRent.External` (10 min)
  - Implementation: `src/MotoRent.Server/Program.cs`

**API Authentication:**
- JWT Bearer Tokens
  - Secret: `MOTO_JwtSecret`
  - Issuer: `MOTO_JwtIssuer` (default: `motorent`)
  - Audience: `MOTO_JwtAudience` (default: `motorent-api`)
  - Expiration: `MOTO_JwtExpirationMonths` (default: 6 months)
  - Implementation: `System.IdentityModel.Tokens.Jwt`

**Super Admin:**
- Config: `MOTO_SuperAdmin` - Comma-separated email list
- Claim: `SuperAdmin` added to authenticated users

## Messaging & Real-time

**Message Broker (Optional):**
- RabbitMQ - Async message processing
  - Host: `appsettings.json` > `RabbitMQ:Host`
  - Enabled: `RabbitMQ:Enabled` (default: false)
  - Virtual Host: `motorent`
  - Exchange: `motorent.topics`
  - Dead Letter: `motorent.dead-letter`
  - Implementation: `src/MotoRent.Messaging/RabbitMqMessageBroker.cs`
  - Consumer: `src/MotoRent.Worker/`

**Real-time (SignalR):**
- Comment Hub - Real-time comment notifications
  - Endpoint: `/hub-comments`
  - Implementation: `src/MotoRent.Server/Hubs/CommentHub.cs`
  - Used for: Broadcasting new comments to connected clients

## Notifications

**Email (SMTP):**
- Generic SMTP support
  - Host: `MOTO_SmtpHost`
  - Port: `MOTO_SmtpPort` (default: 587)
  - Credentials: `MOTO_SmtpUser`, `MOTO_SmtpPassword`
  - From: `MOTO_SmtpFromEmail`, `MOTO_SmtpFromName`
  - Implementation: `src/MotoRent.Services/NotificationService.cs`
  - Templates: Booking confirmation, payment receipt, reminders, cancellation

**LINE Notify:**
- LINE Notify API - Push notifications to LINE users
  - Token: `MOTO_LineNotifyToken` (for shop staff)
  - Endpoint: `https://notify-api.line.me/api/notify`
  - Implementation: `src/MotoRent.Services/NotificationService.cs`
  - Used for: Booking confirmations, payment receipts, staff alerts

## Monitoring & Observability

**Error Tracking:**
- Custom SQL Logger
  - Implementation: `SqlLogger` class
  - Storage: `[Core].[LogEntry]` table
  - Middleware: `UseExceptionLogging()` in Program.cs
  - Service: `src/MotoRent.Services/Core/LogEntryService.cs`

**Logs:**
- Microsoft.Extensions.Logging
  - Console logging for development
  - Configurable log levels in `appsettings.json`

## CI/CD & Deployment

**Hosting:**
- Self-hosted or cloud (Azure App Service compatible)
- Docker support via `containers/` directory (inherited from main forex project)

**CI Pipeline:**
- GitHub repository: `https://github.com/erymuzuan/motorent`
- Build scripts: PowerShell (`build.web.ps1`)

## Environment Configuration

**Required env vars (minimum):**
```
MOTO_SqlConnectionString    # SQL Server connection
MOTO_JwtSecret              # JWT signing key (change from default!)
MOTO_SuperAdmin             # Admin email addresses
```

**Recommended for production:**
```
MOTO_GoogleClientId         # Google OAuth
MOTO_GoogleClientSecret
MOTO_MicrosoftClientId      # Microsoft OAuth
MOTO_MicrosoftClientSecret
MOTO_GeminiApiKey           # Document OCR
AWS_ACCESS_KEY_ID           # S3 storage
AWS_SECRET_ACCESS_KEY
AWS_REGION
MOTO_SmtpHost               # Email notifications
MOTO_SmtpUser
MOTO_SmtpPassword
```

**Thai market specific:**
```
MOTO_LineChannelId          # LINE OAuth
MOTO_LineChannelSecret
MOTO_LineNotifyToken        # LINE Notify for staff
```

**Secrets location:**
- Environment variables (recommended)
- `appsettings.json` (development only, not for production secrets)
- PowerShell script: `env.motorent.ps1` (local development)

## Webhooks & Callbacks

**Incoming:**
- `/signin-google` - Google OAuth callback
- `/signin-microsoft` - Microsoft OAuth callback
- `/signin-line` - LINE OAuth callback
- `/hub-comments` - SignalR WebSocket endpoint

**Outgoing:**
- Google Gemini API (document OCR)
- LINE Notify API (push notifications)
- SMTP (email)
- OpenSearch (if configured)

## Multi-Tenant Architecture

**Tenant Resolution:**
- URL-based: `/tourist/{accountNo}/` paths use `TouristRequestContext`
- Claims-based: Authenticated pages use `MotoRentRequestContext`
- Domain-based: Custom domain/subdomain resolution via `UseTenantDomainResolution()` middleware

**Tenant Data Isolation:**
- Core entities: `[Core]` schema (shared)
- Tenant entities: `[{AccountNo}]` schema (isolated)
- Implementation: `src/MotoRent.Services/Core/SqlSubscriptionService.cs`

---

*Integration audit: 2026-01-23*
