using System.Data;
using System.Linq.Expressions;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using Npgsql;
using Polly;
using Polly.Retry;

namespace MotoRent.PostgreSqlRepository;

public partial class PgJsonRepository<T> where T : Entity, new()
{
    public async Task<int> GetCountAsync(IQueryable<T> query)
    {
        // Use * for subquery compatibility (computed columns must be visible in outer queries)
        var sql = query.ToString()!.Replace("\"Data\"", "*");
        // Strip ORDER BY - invalid with COUNT(*) without GROUP BY
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
            sql = sql[..orderByIndex].TrimEnd();

        sql = $"SELECT COUNT(*) FROM ({sql}) AS _cnt";
        return await this.GetCountAsync(sql);
    }

    private async Task<int> GetCountAsync(string sql)
    {
        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result);
        });
    }

    public async Task<bool> ExistAsync(IQueryable<T> query)
    {
        return (await this.GetCountAsync(query)) > 0;
    }

    public async Task<TResult> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector) where TResult : struct
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0) sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT COALESCE(SUM(\"{column}\"), 0) FROM ({sql}) AS _agg";
        return await this.ExecuteScalarAsync<TResult>(sql);
    }

    public async Task<TResult?> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult?>> selector) where TResult : struct
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0) sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT SUM(\"{column}\") FROM ({sql}) AS _agg";
        return await this.ExecuteScalarNullableAsync<TResult>(sql);
    }

    public async Task<TResult> GetMaxAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0) sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT MAX(\"{column}\") FROM ({sql}) AS _agg";
        return await this.ExecuteScalarAsync<TResult>(sql);
    }

    public async Task<TResult> GetMinAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0) sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT MIN(\"{column}\") FROM ({sql}) AS _agg";
        return await this.ExecuteScalarAsync<TResult>(sql);
    }

    public async Task<decimal> GetAverageAsync(IQueryable<T> query, Expression<Func<T, decimal>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0) sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT COALESCE(AVG(CAST(\"{column}\" AS NUMERIC(18,4))), 0) FROM ({sql}) AS _agg";
        return await this.ExecuteScalarAsync<decimal>(sql);
    }

    public async Task<TResult?> GetScalarAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        sql = $"SELECT \"{column}\" FROM ({sql}) AS _s";
        // Add LIMIT 1 if not already present
        if (!sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase))
            sql += " LIMIT 1";
        return await this.ExecuteScalarNullableAsync<TResult>(sql);
    }

    public async Task<List<T>> GetListAsync(IQueryable<T> query, Expression<Func<T, object>> selector)
    {
        var result = await this.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<List<TResult>> GetDistinctAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the column name");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0) sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT DISTINCT \"{column}\" FROM ({sql}) AS _d";
        return await this.ExecuteListAsync<TResult>(sql);
    }

    public async Task<List<(TKey Key, int Count)>> GetGroupCountAsync<TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector)
    {
        var keyColumn = GetMemberName(keySelector);
        if (string.IsNullOrWhiteSpace(keyColumn))
            throw new ArgumentException("Cannot determine the key column name");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
            sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT \"{keyColumn}\", COUNT(*) as \"Cnt\" FROM ({sql}) AS _g GROUP BY \"{keyColumn}\"";

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var results = new List<(TKey, int)>();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var key = ConvertToType<TKey>(reader.GetValue(0));
                var count = reader.GetInt32(1);
                if (key != null)
                    results.Add((key, count));
            }
            return results;
        });
    }

    public async Task<List<(TKey Key, TValue Sum)>> GetGroupSumAsync<TKey, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TValue>> valueSelector)
    {
        var keyColumn = GetMemberName(keySelector);
        var valueColumn = GetMemberName(valueSelector);
        if (string.IsNullOrWhiteSpace(keyColumn) || string.IsNullOrWhiteSpace(valueColumn))
            throw new ArgumentException("Cannot determine column names");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
            sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT \"{keyColumn}\", SUM(\"{valueColumn}\") as \"Total\" FROM ({sql}) AS _g GROUP BY \"{keyColumn}\"";

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var results = new List<(TKey, TValue)>();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var key = ConvertToType<TKey>(reader.GetValue(0));
                var sum = ConvertToType<TValue>(reader.GetValue(1));
                if (key != null && sum != null)
                    results.Add((key, sum));
            }
            return results;
        });
    }

    public async Task<List<(TKey Key, TKey2 Key2, TValue Sum)>> GetGroupSumAsync<TKey, TKey2, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TKey2>> key2Selector,
        Expression<Func<T, TValue>> valueSelector)
    {
        var keyColumn = GetMemberName(keySelector);
        var key2Column = GetMemberName(key2Selector);
        var valueColumn = GetMemberName(valueSelector);
        if (string.IsNullOrWhiteSpace(keyColumn) || string.IsNullOrWhiteSpace(key2Column) || string.IsNullOrWhiteSpace(valueColumn))
            throw new ArgumentException("Cannot determine column names");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
            sql = sql[..orderByIndex].TrimEnd();
        sql = $"SELECT \"{keyColumn}\", \"{key2Column}\", SUM(\"{valueColumn}\") as \"Total\" FROM ({sql}) AS _g GROUP BY \"{keyColumn}\", \"{key2Column}\"";

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var results = new List<(TKey, TKey2, TValue)>();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var key = ConvertToType<TKey>(reader.GetValue(0));
                var key2 = ConvertToType<TKey2>(reader.GetValue(1));
                var sum = ConvertToType<TValue>(reader.GetValue(2));
                if (key != null && key2 != null && sum != null)
                    results.Add((key, key2, sum));
            }
            return results;
        });
    }

    public async Task<List<(TResult, TResult2)>> GetList2Async<TResult, TResult2>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2)
    {
        var column1 = GetMemberName(selector);
        var column2 = GetMemberName(selector2);
        if (string.IsNullOrWhiteSpace(column1) || string.IsNullOrWhiteSpace(column2))
            throw new ArgumentException("Cannot determine column names");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        sql = $"SELECT \"{column1}\", \"{column2}\" FROM ({sql}) AS _l";

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var results = new List<(TResult, TResult2)>();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var val1 = ConvertToType<TResult>(reader.GetValue(0));
                var val2 = ConvertToType<TResult2>(reader.GetValue(1));
                if (val1 != null && val2 != null)
                    results.Add((val1, val2));
            }
            return results;
        });
    }

    public async Task<List<(TResult, TResult2, TResult3)>> GetList3Async<TResult, TResult2, TResult3>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2,
        Expression<Func<T, TResult3>> selector3)
    {
        var column1 = GetMemberName(selector);
        var column2 = GetMemberName(selector2);
        var column3 = GetMemberName(selector3);
        if (string.IsNullOrWhiteSpace(column1) || string.IsNullOrWhiteSpace(column2) || string.IsNullOrWhiteSpace(column3))
            throw new ArgumentException("Cannot determine column names");

        var sql = query.ToString()!.Replace("\"Data\"", "*");
        sql = $"SELECT \"{column1}\", \"{column2}\", \"{column3}\" FROM ({sql}) AS _l";

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var results = new List<(TResult, TResult2, TResult3)>();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var val1 = ConvertToType<TResult>(reader.GetValue(0));
                var val2 = ConvertToType<TResult2>(reader.GetValue(1));
                var val3 = ConvertToType<TResult3>(reader.GetValue(2));
                if (val1 != null && val2 != null && val3 != null)
                    results.Add((val1, val2, val3));
            }
            return results;
        });
    }

    private async Task<TResult> ExecuteScalarAsync<TResult>(string sql)
    {
        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync(ct);
            return ConvertToType<TResult>(result) ?? default!;
        });
    }

    private async Task<TResult?> ExecuteScalarNullableAsync<TResult>(string sql)
    {
        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync(ct);
            return ConvertToType<TResult>(result);
        });
    }

    private async Task<List<TResult>> ExecuteListAsync<TResult>(string sql)
    {
        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var results = new List<TResult>();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var val = ConvertToType<TResult>(reader.GetValue(0));
                if (val != null)
                    results.Add(val);
            }
            return results;
        });
    }
}
