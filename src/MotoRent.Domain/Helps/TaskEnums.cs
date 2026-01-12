namespace MotoRent.Domain.Helps;

public enum TaskStatus
{
    Scheduled,
    Ready,
    InProgress,
    Done,
    Partial,
    Cancelled,
    Missed,
    Rescheduled,
    Provisioned,
    Error
}

public enum TaskPriority
{
    None,
    Low,
    Medium,
    High
}

public enum Difficulty
{
    None,
    Low,
    Medium,
    High
}
