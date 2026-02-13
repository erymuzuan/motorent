using System.Text;
using MotoRent.Domain.Entities;
using MotoRent.Core.Repository;

namespace MotoRent.PostgreSqlRepository;

public class PgPagingTranslator : ICorePagingTranslator
{
    public string Translate(string sql, int page, int size)
    {
        var skipToken = (page - 1) * size;
        var output = new StringBuilder(sql);

        output.AppendLine();
        output.AppendFormat("LIMIT {0} OFFSET {1}", size, skipToken);

        return output.ToString();
    }

    public string Translate<T>(string sql, int page, int size) where T : Entity, new()
    {
        return this.Translate(sql, page, size);
    }

    public string TranslateWithSkip(string sql, int top, int skip)
    {
        var output = new StringBuilder(sql);

        output.AppendLine();
        output.AppendLine($"LIMIT {top} OFFSET {skip}");

        return output.ToString();
    }
}
