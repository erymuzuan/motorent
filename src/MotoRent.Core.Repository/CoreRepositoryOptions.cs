namespace MotoRent.Core.Repository;

/// <summary>
/// Configuration options for Core repository.
/// </summary>
public class CoreRepositoryOptions
{
    public const string SectionName = "CoreRepository";

    /// <summary>
    /// Connection string for the Core database (SQL Server).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
