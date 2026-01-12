namespace MotoRent.Messaging;

/// <summary>
/// Configuration manager for RabbitMQ settings from environment variables.
/// </summary>
public static class RabbitMqConfigurationManager
{
    private const string APPLICATION_NAME = "MOTORENT";

    public static string UserName => GetEnvironmentVariable("RabbitMqUserName") ?? "guest";
    public static string Password => GetEnvironmentVariable("RabbitMqPassword") ?? "guest";
    public static string Host => GetEnvironmentVariable("RabbitMqHost") ?? "localhost";
    public static string ManagementScheme => GetEnvironmentVariable("RabbitMqManagementScheme") ?? "http";
    public static int Port => GetEnvironmentVariableInt32("RabbitMqPort", 5672);
    public static int ManagementPort => GetEnvironmentVariableInt32("RabbitMqManagementPort", 15672);
    public static string VirtualHost => GetEnvironmentVariable("RabbitMqVirtualHost") ?? "motorent";
    public static string DefaultExchange => GetEnvironmentVariable("RabbitMqDefaultExchange") ?? "motorent.topics";
    public static string DefaultDeadLetterQueue => GetEnvironmentVariable("RabbitMqDefaultDeadLetterQueue") ?? "motorent_dead_letter_queue";
    public static string DefaultDeadLetterExchange => GetEnvironmentVariable("RabbitMqDefaultDeadLetterExchange") ?? "motorent.dead-letter";

    public static int GetEnvironmentVariableInt32(string setting, int defaultValue = 0)
    {
        var val = GetEnvironmentVariable(setting);
        return int.TryParse(val, out var intValue) ? intValue : defaultValue;
    }

    public static bool GetEnvironmentVariableBoolean(string setting, bool defaultValue = false)
    {
        var val = GetEnvironmentVariable(setting);
        return bool.TryParse(val, out var boolValue) ? boolValue : defaultValue;
    }

    public static string? GetEnvironmentVariable(string setting, bool isOptional = true)
    {
        // Check process-level first
        var process = Environment.GetEnvironmentVariable($"{APPLICATION_NAME}_{setting}", EnvironmentVariableTarget.Process);
        if (!string.IsNullOrWhiteSpace(process)) return process;

        // Check user-level
        var user = Environment.GetEnvironmentVariable($"{APPLICATION_NAME}_{setting}", EnvironmentVariableTarget.User);
        if (!string.IsNullOrWhiteSpace(user)) return user;

        // Check machine-level
        return Environment.GetEnvironmentVariable($"{APPLICATION_NAME}_{setting}", EnvironmentVariableTarget.Machine);
    }
}
