using System.Diagnostics;

namespace MotoRent.SqlServerRepository;

[DebuggerDisplay("{Name} {SqlType} Write-{CanWrite}")]
public class Column
{
    public string? Name { get; set; }
    public string? SqlType { get; set; }
    public int Length { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool CanWrite { get; set; }

    public string GetParameterName(int count) => this.Name?.IndexOf('.') > -1
        ? $"@{this.Name}{count}".Replace(".", "_")
        : $"@{this.Name}{count}";
}
