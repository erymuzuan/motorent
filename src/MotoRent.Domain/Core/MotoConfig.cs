using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

namespace MotoRent.Domain.Core;

/// <summary>
/// Centralized configuration manager that reads from environment variables with MOTO_ prefix.
/// Falls back to default values if environment variables are not set.
/// </summary>
public static class MotoConfig
{
    private const string PREFIX = "MOTO_";

    // Connection Strings
    public static string SqlConnectionString => GetEnvironmentVariable("SqlConnectionString") ??
                                                "Data Source=.\\DEV2022;Initial Catalog=MotoRent;Integrated Security=True;TrustServerCertificate=True;Application Name=MotoRent";

    // Authentication - Google OAuth
    public static string? GoogleClientId => GetEnvironmentVariable("GoogleClientId");
    public static string? GoogleClientSecret => GetEnvironmentVariable("GoogleClientSecret");

    // Authentication - Microsoft OAuth
    public static string? MicrosoftClientId => GetEnvironmentVariable("MicrosoftClientId");
    public static string? MicrosoftClientSecret => GetEnvironmentVariable("MicrosoftClientSecret");

    // Authentication - LINE OAuth
    public static string? LineChannelId => GetEnvironmentVariable("LineChannelId");
    public static string? LineChannelSecret => GetEnvironmentVariable("LineChannelSecret");

    // JWT Configuration
    public static string JwtSecret =>
        GetEnvironmentVariable("JwtSecret") ?? "motorent-default-jwt-secret-key-change-in-production";

    public static string JwtIssuer => GetEnvironmentVariable("JwtIssuer") ?? "motorent";
    public static string JwtAudience => GetEnvironmentVariable("JwtAudience") ?? "motorent-api";
    public static int JwtExpirationMonths => GetEnvironmentVariableInt32("JwtExpirationMonths", 6);

    // Super Admin
    public static string[] SuperAdmins => (GetEnvironmentVariable("SuperAdmin") ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // Gemini OCR Configuration
    public static string? GeminiApiKey => GetEnvironmentVariable("GeminiApiKey");
    public static string GeminiModel => GetEnvironmentVariable("GeminiModel") ?? "gemini-3-flash-preview";

    // Google Maps Configuration
    public static string? GoogleMapKey => GetEnvironmentVariable("GoogleMapKey");

    // File Storage
    public static string FileStorageBasePath => GetEnvironmentVariable("FileStorageBasePath") ?? "uploads";
    public static int FileStorageMaxSizeMb => GetEnvironmentVariableInt32("FileStorageMaxSizeMb", 10);

    // AWS S3 Configuration
    public static string? AwsAccessKeyId => GetEnvironmentVariable("AWS_ACCESS_KEY_ID", false);
    public static string? AwsSecretAccessKey => GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", false);
    public static string AwsRegion => GetEnvironmentVariable("AWS_REGION", false) ?? "ap-southeast-1";
    public static string AwsBucket => GetEnvironmentVariable("AwsBucket") ?? "motorent.private";
    public static string AwsPublicBucket => GetEnvironmentVariable("AwsPublicBucket") ?? "motorent.public";

    public static TimeSpan AwsS3UrlTtl => TimeSpan.TryParse(GetEnvironmentVariable("AwsS3Ttl"), out var ts)
        ? ts
        : TimeSpan.FromMinutes(5);

    // Database Scripts Source
    public static string DatabaseSource => GetEnvironmentVariable("DatabaseSource") ?? "database";

    // Application Settings
    public static string ApplicationName => GetEnvironmentVariable("ApplicationName") ?? "MotoRent";
    public static string BaseUrl => GetEnvironmentVariable("BaseUrl") ?? "https://localhost:7103";

    // SMTP Email Configuration
    public static string? SmtpHost => GetEnvironmentVariable("SmtpHost");
    public static int SmtpPort => GetEnvironmentVariableInt32("SmtpPort", 587);
    public static string? SmtpUser => GetEnvironmentVariable("SmtpUser");
    public static string? SmtpPassword => GetEnvironmentVariable("SmtpPassword");
    public static string SmtpFromEmail => GetEnvironmentVariable("SmtpFromEmail") ?? "noreply@motorent.com";
    public static string SmtpFromName => GetEnvironmentVariable("SmtpFromName") ?? "MotoRent";

    // LINE Notify Configuration (for shop staff notifications)
    public static string? LineNotifyToken => GetEnvironmentVariable("LineNotifyToken");

    #region Helper Methods

    public static string? GetEnvironmentVariable(string setting, bool usePrefix = true)
    {
        var prefix = usePrefix ? PREFIX : "";

        // Check Process level first (highest priority)
        var process = Environment.GetEnvironmentVariable($"{prefix}{setting}",
            EnvironmentVariableTarget.Process);
        if (!string.IsNullOrWhiteSpace(process)) return process;

        // Check User level
        var user = Environment.GetEnvironmentVariable($"{prefix}{setting}",
            EnvironmentVariableTarget.User);
        if (!string.IsNullOrWhiteSpace(user)) return user;

        // Check Machine level (lowest priority)
        return Environment.GetEnvironmentVariable($"{prefix}{setting}",
            EnvironmentVariableTarget.Machine);
    }

    public static int GetEnvironmentVariableInt32(string setting, int defaultValue = 0)
    {
        var val = GetEnvironmentVariable(setting);
        return int.TryParse(val, out var intValue) ? intValue : defaultValue;
    }

    public static decimal GetEnvironmentVariableDecimal(string setting, decimal defaultValue = 0m)
    {
        var val = GetEnvironmentVariable(setting);
        return decimal.TryParse(val, out var decValue) ? decValue : defaultValue;
    }

    public static bool GetEnvironmentVariableBoolean(string setting, bool defaultValue = false)
    {
        var val = GetEnvironmentVariable(setting);
        return bool.TryParse(val, out var boolValue) ? boolValue : defaultValue;
    }

    #endregion

    
}