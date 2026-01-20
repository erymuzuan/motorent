using MotoRent.Domain.Entities;

namespace MotoRent.Core.Repository;

public interface ICorePagingTranslator
{
    string Translate<T>(string sql, int page, int size) where T : Entity, new();
    string TranslateWithSkip(string sql, int top, int skip);
}
