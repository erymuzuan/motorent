namespace MotoRent.PostgreSqlRepository;

public class PgTable
{
    public string Name { get; set; } = "";
    public PgColumn[] Columns { get; set; } = [];
}
