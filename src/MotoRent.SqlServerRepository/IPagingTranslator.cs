namespace MotoRent.SqlServerRepository;

public interface IPagingTranslator
{
    string Translate(string sql, int page, int size);
    string TranslateWithSkip(string sql, int top, int skip);
}
