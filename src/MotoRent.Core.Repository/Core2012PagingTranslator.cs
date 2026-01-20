using System.Text;
using MotoRent.Domain.Entities;

namespace MotoRent.Core.Repository;

public class Core2012PagingTranslator : ICorePagingTranslator
{
    public string Translate<T>(string sql, int page, int size) where T : Entity, new()
    {
        var skipToken = (page - 1) * size;
        var output = new StringBuilder(sql);

        output.AppendLine();
        output.AppendFormat("OFFSET {0} ROWS", skipToken);
        output.AppendLine();
        output.AppendFormat("FETCH NEXT {0} ROWS ONLY", size);

        return output.ToString();
    }

    public string TranslateWithSkip(string sql, int top, int skip)
    {
        var output = new StringBuilder(sql);

        output.AppendLine();
        output.AppendLine($"OFFSET {skip} ROWS");
        output.AppendLine($"FETCH NEXT {top} ROWS ONLY");

        return output.ToString();
    }
}
