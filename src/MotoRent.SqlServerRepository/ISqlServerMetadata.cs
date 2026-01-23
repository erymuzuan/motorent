namespace MotoRent.SqlServerRepository;

public interface ISqlServerMetadata
{
    Task<Table?> GetTableAsync(string account, string name);
}
