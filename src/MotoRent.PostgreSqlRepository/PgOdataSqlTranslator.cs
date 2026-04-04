using System.Text.RegularExpressions;

namespace MotoRent.PostgreSqlRepository;

public class PgOdataSqlTranslator(string column, string table)
{
    private string Column { get; } = column;
    private string Table { get; } = table;

    public string Translate(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return "";
        var input = filter
            .Replace(" ne null", " IS NOT NULL")
            .Replace(" eq null", " IS NULL")
            .Replace(" ne ", " <> ")
            .Replace(" eq ", " = ")
            .Replace(" gt ", " > ")
            .Replace(" ge ", " >= ")
            .Replace(" gte ", " >= ")
            .Replace(" lt ", " < ")
            .Replace(" lte ", " <= ")
            .Replace(" le ", " <= ")
            .Replace("(From ", "(\"From\" ")
            .Replace("(To ", "(\"To\" ")
            .Replace("DateTime'", "'");

        var output = Regex.Replace(input, @"^([\w\-]+)", m => "\"" + m + "\"");
        output = Regex.Replace(output, @"startswith\((?<f>[\w\-]+),'(?<v>[\w\-]+)'\) = true",
            m => $"\"{m.Groups["f"]}\" LIKE '{m.Groups["v"]}%'");
        output = Regex.Replace(output, @"startswith\((?<f>[\w\-]+),'(?<v>[\w\-]+)'\) = false",
            m => $"\"{m.Groups["f"]}\" NOT LIKE '{m.Groups["v"]}%'");
        output = Regex.Replace(output, @"endswith\((?<f>[\w\-]+),'(?<v>[\w\-]+)'\) = true",
            m => $"\"{m.Groups["f"]}\" LIKE '%{m.Groups["v"]}'");
        output = Regex.Replace(output, @"endswith\((?<f>[\w\-]+),'(?<v>[\w\-]+)'\) = false",
            m => $"\"{m.Groups["f"]}\" NOT LIKE '%{m.Groups["v"]}'");
        output = Regex.Replace(output, @" and ([\w\-]+)",
            m => " AND \"" + m.ToString().Replace(" and ", string.Empty) + "\"");
        output = Regex.Replace(output, @" or ([\w\-]+)",
            m => " OR \"" + m.ToString().Replace(" or ", string.Empty) + "\"");
        output = output.Replace(" = DateTime ", " \"DateTime\" ");
        output = output.Replace(" = true", " = true");
        output = output.Replace(" = false", " = false");
        output = Regex.Replace(output, @"\s(?<andor>and|or|AND|OR)\s (?<f>[\w\-]+)",
            m => $"{m.Groups["andor"]} \"{m.Groups["f"]}\"");
        return " WHERE " + output;
    }

    public string Max(string filter)
    {
        return $"SELECT MAX(\"{this.Column}\") FROM \"{this.Table}\" " +
               this.Translate(filter);
    }

    public string Min(string filter)
    {
        return $"SELECT MIN(\"{this.Column}\") FROM \"{this.Table}\" " +
               this.Translate(filter);
    }

    public string Average(string filter)
    {
        return $"SELECT AVG(\"{this.Column}\") FROM \"{this.Table}\" " +
               this.Translate(filter);
    }

    public string Count(string filter)
    {
        return $"SELECT COUNT(*) FROM \"{this.Table}\"  " +
               this.Translate(filter);
    }

    public string Sum(string filter)
    {
        return $"SELECT SUM(\"{this.Column}\") FROM \"{this.Table}\"  " +
               this.Translate(filter);
    }

    public string Scalar(string filter)
    {
        return $"SELECT \"{this.Column}\" FROM \"{this.Table}\" {this.Translate(filter)} ";
    }

    public string Select(string? filter = null)
    {
        var sql = $"SELECT \"{this.Table}Id\",\"Json\" FROM \"{this.Table}\"";
        if (string.IsNullOrEmpty(filter))
            return sql;
        return $"{sql} {this.Translate(filter)} ";
    }
}
