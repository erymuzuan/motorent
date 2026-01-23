# External Integrations

**Analysis Date:** 2026-01-19

## APIs & External Services

**AI/ML:**
- Google Gemini API - Document OCR and vehicle recognition
  - SDK/Client: `IHttpClientFactory` named "Gemini"
  - Auth: `MOTO_GeminiApiKey` env var
  - Model: `MOTO_GeminiModel` (default: `gemini-3-flash-preview`)
  - Implementation: `src/MotoRent.Services/DocumentOcrService.cs`
  - Implementation: `src/MotoRent.Services/VehicleRecognitionService.cs`

**Mapping:**
- Google Maps - Location services
  - Client: JS Interop via `GoogleMapJsInterop.cs`
  - Auth: `MOTO_GoogleMapKey` env var

**Search:**
- OpenSearch - Full-text search (optional)
  - SDK/Client: `IHttpClientFactory` named "OpenSearchHost"
  - Auth: `MOTO_OpenSearchBasicAuth` env var (Basic Auth)
  - Host: `MOTO_OpenSearchHost` env var
  - Implementation: `src/MotoRent.Services/Search/OpenSearchService.cs`
  - Multi-tenant indexes: `{accountNo}_{EntityType}` pattern

## Data Storage

**Primary Database:**
- Microsoft SQL Server 2019+
  - Connection: `MOTO_SqlConnectionString` env var
  - ORM: Custom Repository Pattern (`RentalDataContext`)
  - JSON columns with computed columns for indexing
  - Multi-tenant via schema separation (`[Core]`, `[AccountNo]`)
  - Schema scripts: `database/*.sql`

**File Storage:**
- AWS S3 - Binary/document storage
  - SDK: `AWSSDK.S3 4.0.17`
  - Client: `S3BinaryStore` (`src/MotoRent.Services/Storage/S3BinaryStore.cs`)
  - Auth: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` env vars
  - Region: `AWS_REGION` (default: `ap-southeast-1`)
  - Private bucket: `MOTO_AwsBucket` (default: `motorent.private`)
  - Public bucket: `MOTO_AwsPublicBucket` (default: `motorent.public`)
  - Pre-signed URLs for private access
  - TTL: `MOTO_AwsS3Ttl` (default: 5 minutes)

**Local File Storage:**
- Fallback for development
  - Path: `MOTO_FileStorageBasePath` (default: `uploads`)
  - Max size: `MOTO_FileStorageMaxSizeMb` (default: 10MB)

**Caching:**
- None detected (no Redis/Memcached)

## Authentication & Identity

**OAuth 2.0 Providers:**

1. Google OAuth
   - SDK: `Microsoft.AspNetCore.Authentication.Google`
   - Client ID: `MOTO_GoogleClientId`
   - Client Secret: `MOTO_GoogleClientSecret`
   - Callback: `/signin-google`

2. Microsoft OAuth
   - SDK: `Microsoft.AspNetCore.Authentication.MicrosoftAccount`
   - Client ID: `MOTO_MicrosoftClientId`
   - Client Secret: `MOTO_MicrosoftClientSecret`
   - Callback: `/signin-microsoft`

3. LINE OAuth (Thailand market)
   - SDK: `AspNet.Security.OAuth.Line`
   - Channel ID: `MOTO_LineChannelId`
   - Channel Secret: `MOTO_LineChannelSecret`
   - Callback: `/signin-line`
   - Scopes: `profile`, `openid`

**Session Management:**
- Cookie-based authentication
  - Name: `MotoRent.Auth`
  - Expiration: 14 days sliding
  - External auth cookie: `MotoRent.External` (10 min)

**JWT Tokens:**
- For API access tokens
  - Secret: `MOTO_JwtSecret`
  - Issuer: `MOTO_JwtIssuer` (default: `motorent`)
  - Audience: `MOTO_JwtAudience` (default: `motorent-api`)
  - Expiration: `MOTO_JwtExpirationMonths` (default: 6 months)
  - SDK: `System.IdentityModel.Tokens.Jwt`

**Super Admin:**
- Configured via `MOTO_SuperAdmin` (comma-separated emails)

## Messaging & Notifications

**Email (SMTP):**
- Standard SMTP with TLS
  - Host: `MOTO_SmtpHost`
  - Port: `MOTO_SmtpPort` (default: 587)
  - User: `MOTO_SmtpUser`
  - Password: `MOTO_SmtpPassword`
  - From: `MOTO_SmtpFromEmail`, `MOTO_SmtpFromName`
  - Implementation: `src/MotoRent.Services/NotificationService.cs`

**LINE Notify:**
- Push notifications to LINE users
  - API: `https://notify-api.line.me/api/notify`
  - Token: Per-shop or `MOTO_LineNotifyToken` global
  - Implementation: `src/MotoRent.Services/NotificationService.cs`

**Message Queue (Optional):**
- RabbitMQ for async processing
  - SDK: `RabbitMQ.Client 7.2.0`
  - Config: `RabbitMQ` section in `appsettings.json`
  - Enabled flag: `RabbitMQ:Enabled`
  - Implementation: `src/MotoRent.Messaging/RabbitMqMessageBroker.cs`
  - Subscribers: `src/MotoRent.Worker/Subscribers/`

## Real-Time Communication

**SignalR:**
- Real-time comment notifications
  - Hub: `/hub-comments`
  - Implementation: `src/MotoRent.Server/Hubs/CommentHub.cs`
  - Worker client: `src/MotoRent.Worker/Subscribers/CommentSignalRSubscriber.cs`

## Monitoring & Observability

**Error Tracking:**
- Custom SQL-based logging
  - Implementation: `SqlLogger` class
  - Service: `LogEntryService`
  - Table: `[Core].[LogEntry]`

**Logs:**
- Microsoft.Extensions.Logging (console + structured)
- Log levels configured in `appsettings.json`

## CI/CD & Deployment

**Hosting:**
- Not specified (no Dockerfile detected)
- PowerShell build scripts present

**CI Pipeline:**
- GitHub repository: `https://github.com/erymuzuan/motorent`
- GitHub Actions: `.github/` directory present

## Environment Configuration

**Required env vars (minimum):**
```
MOTO_SqlConnectionString     # Database
MOTO_GoogleClientId          # Or another OAuth provider
MOTO_GoogleClientSecret
```

**Required for full functionality:**
```
AWS_ACCESS_KEY_ID            # File storage
AWS_SECRET_ACCESS_KEY
MOTO_GeminiApiKey            # OCR features
MOTO_SmtpHost                # Email notifications
```

**Secrets location:**
- Environment variables (recommended)
- `appsettings.Development.json` (local only, gitignored)
- `env.motorent.local.ps1` (PowerShell script, gitignored)

## Webhooks & Callbacks

**Incoming:**
- OAuth callbacks: `/signin-google`, `/signin-microsoft`, `/signin-line`
- No external webhook endpoints detected

**Outgoing:**
- LINE Notify API calls
- OpenSearch indexing (if enabled)
- SMTP email sending

## Multi-Tenant Architecture

**Tenant Resolution:**
- Claims-based for authenticated users (`MotoRentRequestContext`)
- URL-based for tourist pages (`TouristRequestContext`)
- AccountNo claim identifies tenant

**Schema Separation:**
- `[Core]` - Shared entities (Organization, User, Setting, AccessToken, LogEntry)
- `[{AccountNo}]` - Tenant-specific operational data

---

*Integration audit: 2026-01-19*
