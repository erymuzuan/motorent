namespace MotoRent.Domain.DataContext;

public class SubmitOperation
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public List<Exception> Errors { get; set; } = [];

    public static SubmitOperation CreateSuccess(int inserted = 0, int updated = 0, int deleted = 0)
    {
        return new SubmitOperation
        {
            Success = true,
            InsertedCount = inserted,
            UpdatedCount = updated,
            DeletedCount = deleted
        };
    }

    public static SubmitOperation CreateFailure(string message, Exception? exception = null)
    {
        var op = new SubmitOperation
        {
            Success = false,
            Message = message
        };
        if (exception != null)
            op.Errors.Add(exception);
        return op;
    }
}
