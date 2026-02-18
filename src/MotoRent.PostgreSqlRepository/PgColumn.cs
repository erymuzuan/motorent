namespace MotoRent.PostgreSqlRepository;

public class PgColumn
{
    public string Name { get; set; } = "";
    public string SqlType { get; set; } = "";
    public int Length { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool CanWrite { get; set; }

    public string GetParameterName(int count)
    {
        return $"@{this.Name}{count}".Replace(".", "_");
    }
}
