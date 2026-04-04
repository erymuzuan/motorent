namespace MotoRent.PostgreSqlRepository;

public interface IPgMetadata
{
    PgTable GetTable(string name);
}
