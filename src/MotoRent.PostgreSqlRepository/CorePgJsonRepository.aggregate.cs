using System.Data;
using System.Linq.Expressions;
using MotoRent.Domain.Entities;
using Npgsql;
using Polly;
using Polly.Retry;

namespace MotoRent.PostgreSqlRepository;

public partial class CorePgJsonRepository<T> where T : Entity, new()
{
    public async Task<int> GetCountAsync(IQueryable<T> query)
    {
        var sql = query.ToString()!.Replace("\"Data\"", "COUNT(*)");
        // Strip ORDER BY - invalid with COUNT(*) without GROUP BY
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
            sql = sql[..orderByIndex].TrimEnd();
        return await this.GetCountAsync(sql);
    }

    private async Task<int> GetCountAsync(string sql)
    {
        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var count = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(count);
        });
    }

    public async Task<bool> ExistAsync(IQueryable<T> query)
    {
        return await this.GetCountAsync(query) > 0;
    }

    public async Task<TResult> GetMaxAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"MAX(\"{column}\")");

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var max = await cmd.ExecuteScalarAsync(ct);
            if (max is TResult t) return t;
            return default!;
        });
    }

    public async Task<TResult> GetMinAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"MIN(\"{column}\")");

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var min = await cmd.ExecuteScalarAsync(ct);
            if (min is TResult t) return t;
            return default!;
        });
    }

    public async Task<TResult> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
        where TResult : struct
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"SUM(\"{column}\")");

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var sum = await cmd.ExecuteScalarAsync(ct);
            if (sum is TResult t) return t;
            return default;
        });
    }

    public async Task<TResult?> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult?>> selector)
        where TResult : struct
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"SUM(\"{column}\")");

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var sum = await cmd.ExecuteScalarAsync(ct);
            if (sum is TResult t) return t;
            return default(TResult?);
        });
    }

    public async Task<decimal> GetAverageAsync(IQueryable<T> query, Expression<Func<T, decimal>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"AVG(\"{column}\")");

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var avg = await cmd.ExecuteScalarAsync(ct);
            if (avg is decimal d) return d;
            return 0m;
        });
    }

    public async Task<TResult?> GetScalarAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"\"{column}\"");
        // Add LIMIT 1 if not already present
        if (!sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase))
            sql += " LIMIT 1";

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            var scalar = await cmd.ExecuteScalarAsync(ct);
            if (scalar is TResult t) return t;
            return default;
        });
    }

    public async Task<List<T>> GetListAsync(IQueryable<T> query, Expression<Func<T, object>> selector)
    {
        var result = await LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<List<TResult>> GetDistinctAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"DISTINCT \"{column}\"");

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var list = new List<TResult>();
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (reader[0] is not DBNull)
                    list.Add((TResult)reader[0]);
            }
            return list;
        });
    }

    public async Task<List<(TKey Key, int Count)>> GetGroupCountAsync<TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector)
    {
        var column = GetMemberName(keySelector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"\"{column}\", COUNT(*)");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
            sql = sql[..orderByIndex].TrimEnd();
        sql += $"\r\nGROUP BY \"{column}\"";

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var list = new List<(TKey, int)>();
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var keyRaw = reader[0];
                var countRaw = reader[1];
                if (typeof(TKey).IsEnum
                    && Enum.TryParse(typeof(TKey), keyRaw?.ToString(), out var ev)
                    && ev is TKey key1
                    && countRaw is int count)
                {
                    list.Add((key1, count));
                    continue;
                }

                if (keyRaw is TKey key2 && countRaw is int count2)
                {
                    list.Add((key2, count2));
                }
            }
            return list;
        });
    }

    public async Task<List<(TKey Key, TValue Sum)>> GetGroupSumAsync<TKey, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TValue>> valueSelector)
    {
        var column = GetMemberName(keySelector);
        var valueColumn = GetMemberName(valueSelector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        if (string.IsNullOrWhiteSpace(valueColumn))
            throw new ArgumentException("Cannot determine the scalar column name for sum");
        var sql = query.ToString()!.Replace("\"Data\"", $"\"{column}\", SUM(\"{valueColumn}\")");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
            sql = sql[..orderByIndex].TrimEnd();
        sql += $"\r\nGROUP BY \"{column}\"";

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var list = new List<(TKey, TValue)>();
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var keyValue = reader[0];
                var sumObject = reader[1];
                if (typeof(TKey).IsEnum
                    && Enum.TryParse(typeof(TKey), keyValue?.ToString(), out var ev)
                    && ev is TKey actualEnumValue
                    && sumObject is TValue sum)
                {
                    list.Add((actualEnumValue, sum));
                    continue;
                }

                if (keyValue is TKey kv && sumObject is TValue sum1)
                    list.Add((kv, sum1));
            }
            return list;
        });
    }

    public async Task<List<(TKey Key, TKey2 Key2, TValue Sum)>> GetGroupSumAsync<TKey, TKey2, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TKey2>> key2Selector,
        Expression<Func<T, TValue>> valueSelector)
    {
        var column = GetMemberName(keySelector);
        var column2 = GetMemberName(key2Selector);
        var valueColumn = GetMemberName(valueSelector);
        if (string.IsNullOrWhiteSpace(column) || string.IsNullOrWhiteSpace(column2))
            throw new ArgumentException("Cannot determine the scalar column name");
        if (string.IsNullOrWhiteSpace(valueColumn))
            throw new ArgumentException("Cannot determine the scalar column name for sum");

        var sql = query.ToString()!.Replace("\"Data\"", $"\"{column}\", \"{column2}\", SUM(\"{valueColumn}\")");
        var orderByIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
            sql = sql[..orderByIndex].TrimEnd();
        sql += $"\r\nGROUP BY \"{column}\", \"{column2}\"";

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var list = new List<(TKey, TKey2, TValue)>();
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var key1Raw = reader[0];
                var key2Raw = reader[1];
                var sumRaw = reader[2];

                TKey? key1 = default;
                TKey2? key2 = default;

                if (typeof(TKey).IsEnum && Enum.TryParse(typeof(TKey), key1Raw?.ToString(), out var ev1))
                    key1 = (TKey)ev1!;
                else if (key1Raw is TKey k1)
                    key1 = k1;

                if (typeof(TKey2).IsEnum && Enum.TryParse(typeof(TKey2), key2Raw?.ToString(), out var ev2))
                    key2 = (TKey2)ev2!;
                else if (key2Raw is TKey2 k2)
                    key2 = k2;

                if (key1 != null && key2 != null && sumRaw is TValue sum)
                    list.Add((key1, key2, sum));
            }
            return list;
        });
    }

    public async Task<List<(TResult, TResult2)>> GetList2Async<TResult, TResult2>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2)
    {
        var column = GetMemberName(selector);
        var column2 = GetMemberName(selector2);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"\"{column}\", \"{column2}\"");

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var list = new List<(TResult, TResult2)>();
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                TResult? v1 = default;
                TResult2? v2 = default;
                var r1 = reader[0];
                var r2 = reader[1];

                if (r1 is TResult t1) v1 = t1;
                if (r2 is TResult2 t2) v2 = t2;

                if (v1 != null && v2 != null)
                    list.Add((v1, v2));
            }
            return list;
        });
    }

    public async Task<List<(TResult, TResult2, TResult3)>> GetList3Async<TResult, TResult2, TResult3>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2,
        Expression<Func<T, TResult3>> selector3)
    {
        var column = GetMemberName(selector);
        var column2 = GetMemberName(selector2);
        var column3 = GetMemberName(selector3);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        var sql = query.ToString()!.Replace("\"Data\"", $"\"{column}\", \"{column2}\", \"{column3}\"");

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var list = new List<(TResult, TResult2, TResult3)>();
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                TResult? v1 = default;
                TResult2? v2 = default;
                TResult3? v3 = default;
                var r1 = reader[0];
                var r2 = reader[1];
                var r3 = reader[2];

                if (r1 is TResult t1) v1 = t1;
                if (r2 is TResult2 t2) v2 = t2;
                if (r3 is TResult3 t3) v3 = t3;

                if (v1 != null && v2 != null && v3 != null)
                    list.Add((v1, v2, v3));
            }
            return list;
        });
    }

    public async Task<IDataReader> GetReaderAsync(IQueryable<T> query, params string[] columns)
    {
        if (columns.Length == 0)
            throw new ArgumentException("At least one column must be specified", nameof(columns));

        var fields = string.Join(", ", columns.Select(c => $"\"{c}\""));
        var sql = query.ToString()!.Replace("\"Data\"", fields);

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            var cmd = new NpgsqlCommand(sql, conn);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);
        });
    }
}
