# MotoRent Environment Variables Template
# Copy this file to env.motorent.ps1 and fill in your values
# Run with: . .\env.motorent.ps1

# Database Connection
$env:MOTO_SqlConnectionString = "Data Source=.\DEV2022;Initial Catalog=MotoRent;Integrated Security=True;TrustServerCertificate=True;Application Name=MotoRent"

# Google OAuth (get from Google Cloud Console)
$env:MOTO_GoogleClientId = "YOUR_GOOGLE_CLIENT_ID"
$env:MOTO_GoogleClientSecret = "YOUR_GOOGLE_CLIENT_SECRET"

# Microsoft OAuth (get from Azure Portal)
$env:MOTO_MicrosoftClientId = "YOUR_MICROSOFT_CLIENT_ID"
$env:MOTO_MicrosoftClientSecret = "YOUR_MICROSOFT_CLIENT_SECRET"

# JWT Configuration
$env:MOTO_JwtSecret = "your-secure-jwt-secret-key-change-in-production"
$env:MOTO_JwtIssuer = "motorent"
$env:MOTO_JwtAudience = "motorent-api"
$env:MOTO_JwtExpirationMonths = "6"

# Super Admin (comma-separated list of email addresses)
$env:MOTO_SuperAdmin = "admin@example.com"

# Gemini OCR (get from Google AI Studio)
$env:MOTO_GeminiApiKey = "YOUR_GEMINI_API_KEY"
$env:MOTO_GeminiModel = "gemini-2.0-flash"

# File Storage
$env:MOTO_FileStorageBasePath = "uploads"
$env:MOTO_FileStorageMaxSizeMb = "10"

# Database Scripts Source
$env:MOTO_DatabaseSource = "database"

# Application Settings
$env:MOTO_ApplicationName = "MotoRent"
$env:MOTO_BaseUrl = "https://localhost:7103"
