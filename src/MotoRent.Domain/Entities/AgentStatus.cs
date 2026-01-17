namespace MotoRent.Domain.Entities;

/// <summary>
/// Constants for agent status.
/// </summary>
public static class AgentStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Suspended = "Suspended";

    public static readonly string[] All = [Active, Inactive, Suspended];
}
