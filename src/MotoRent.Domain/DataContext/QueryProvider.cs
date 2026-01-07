using Microsoft.Data.SqlClient;

namespace MotoRent.Domain.DataContext;

public class QueryProvider
{
    private readonly string m_connectionString;

    public QueryProvider(string connectionString)
    {
        m_connectionString = connectionString;
    }

    public string ConnectionString => m_connectionString;

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(m_connectionString);
    }
}
