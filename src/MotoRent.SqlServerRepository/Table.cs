using System.Diagnostics;

namespace MotoRent.SqlServerRepository;

[DebuggerDisplay("{Name}")]
public class Table
{
    public string Name { get; set; } = "";
    public Column[] Columns { get; set; } = [];
}
