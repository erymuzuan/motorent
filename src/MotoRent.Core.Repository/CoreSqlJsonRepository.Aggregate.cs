using System.Data;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Entities;
using Polly;

namespace MotoRent.Core.Repository;

public partial class CoreSqlJsonRepository<T> where T : Entity, new()
{
    public async Task<int> GetCountAsync(IQueryable<T> query)
    {
        var sql = query.ToString()!.Replace("[Data]", "COUNT(*)");
        return await this.GetCountAsync(sql);
    }

    private async Task<int> GetCountAsync(string sql)
    {
        var p = Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep);
        var r = await p.ExecuteAndCaptureAsync(async () =>
        {
            await using var conn = new SqlConnection(m_connectionString);
            await using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            var count = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(count);
        });

        if (null != r.FinalException)
            throw r.FinalException;
        return r.Result;
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
        var sql = query.ToString()!.Replace("[Data]", $"MAX([{column}])");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await conn.OpenAsync();
                var max = await cmd.ExecuteScalarAsync();
                if (max is TResult t) return t;
                return default!;
            });
        if (null != pr.FinalException)
            throw pr.FinalException;

        return pr.Result!;
    }

    public async Task<TResult> GetMinAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("[Data]", $"MIN([{column}])");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await conn.OpenAsync();
                var min = await cmd.ExecuteScalarAsync();
                if (min is TResult t) return t;
                return default!;
            });
        if (null != pr.FinalException)
            throw pr.FinalException;

        return pr.Result!;
    }

    public async Task<TResult> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
        where TResult : struct
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("[Data]", $"SUM([{column}])");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await conn.OpenAsync();
                var sum = await cmd.ExecuteScalarAsync();
                if (sum is TResult t) return t;
                return default;
            });
        if (null != pr.FinalException)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<TResult?> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult?>> selector)
        where TResult : struct
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("[Data]", $"SUM([{column}])");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await conn.OpenAsync();
                var sum = await cmd.ExecuteScalarAsync();
                if (sum is TResult t) return t;
                return default(TResult?);
            });
        if (null != pr.FinalException)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<decimal> GetAverageAsync(IQueryable<T> query, Expression<Func<T, decimal>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");
        var sql = query.ToString()!.Replace("[Data]", $"AVG([{column}])");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await conn.OpenAsync();
                var avg = await cmd.ExecuteScalarAsync();
                if (avg is decimal d) return d;
                return 0m;
            });
        if (null != pr.FinalException)
            throw pr.FinalException;
        return pr.Result;
    }

    public async Task<TResult?> GetScalarAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        var sql = query.ToString()!.Replace("[Data]", $"[{column}]");

        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await conn.OpenAsync();
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar is TResult t) return t;
                return default;
            });
        if (null != pr.FinalException)
            throw pr.FinalException;

        return pr.Result;
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
        var sql = query.ToString()!.Replace("[Data]", $"DISTINCT [{column}]");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        var list = new List<TResult>();
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                list.Clear();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    if (reader[0] is not DBNull)
                        list.Add((TResult)reader[0]);
                }
            });
        if (null != pr.FinalException)
            throw pr.FinalException;

        return list;
    }

    public async Task<List<(TKey Key, int Count)>> GetGroupCountAsync<TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector)
    {
        var column = GetMemberName(keySelector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        var sql = query.ToString()!.Replace("[Data]", $"[{column}], COUNT(*)");
        sql += $"\r\nGROUP BY [{column}]";
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        var list = new List<(TKey, int)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
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
        var sql = query.ToString()!.Replace("[Data]", $"[{column}], SUM([{valueColumn}])");

        sql += $"\r\nGROUP BY [{column}]";
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        var list = new List<(TKey, TValue)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
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

        var sql = query.ToString()!.Replace("[Data]", $"[{column}], [{column2}], SUM([{valueColumn}])");
        sql += $"\r\nGROUP BY [{column}], [{column2}]";

        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        var list = new List<(TKey, TKey2, TValue)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
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
    }

    public async Task<List<(TResult, TResult2)>> GetList2Async<TResult, TResult2>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2)
    {
        var column = GetMemberName(selector);
        var column2 = GetMemberName(selector2);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");
        var sql = query.ToString()!.Replace("[Data]", $"[{column}], [{column2}]");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        var list = new List<(TResult, TResult2)>();
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                list.Clear();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    TResult? v1 = default;
                    TResult2? v2 = default;
                    var r1 = reader[0];
                    var r2 = reader[1];

                    if (r1 is TResult t1)
                        v1 = t1;

                    if (r2 is TResult2 t2)
                        v2 = t2;

                    if (v1 != null && v2 != null)
                        list.Add((v1, v2));
                }
            });
        if (null != pr.FinalException)
            throw pr.FinalException;

        return list;
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
        var sql = query.ToString()!.Replace("[Data]", $"[{column}], [{column2}], [{column3}]");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        var list = new List<(TResult, TResult2, TResult3)>();
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                list.Clear();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    TResult? v1 = default;
                    TResult2? v2 = default;
                    TResult3? v3 = default;
                    var r1 = reader[0];
                    var r2 = reader[1];
                    var r3 = reader[2];

                    if (r1 is TResult t1)
                        v1 = t1;

                    if (r2 is TResult2 t2)
                        v2 = t2;

                    if (r3 is TResult3 t3)
                        v3 = t3;

                    if (v1 != null && v2 != null && v3 != null)
                        list.Add((v1, v2, v3));
                }
            });
        if (null != pr.FinalException)
            throw pr.FinalException;

        return list;
    }

    /// <summary>
    /// Gets a data reader for specific columns without loading full entities.
    /// Performance-optimized: no [Json] column, no deserialization.
    /// </summary>
    /// <param name="query">The query to execute</param>
    /// <param name="columns">Column names to select</param>
    /// <returns>IDataReader with CloseConnection behavior - disposes connection when reader is closed</returns>
    public async Task<IDataReader> GetReaderAsync(IQueryable<T> query, params string[] columns)
    {
        if (columns.Length == 0)
            throw new ArgumentException("At least one column must be specified", nameof(columns));

        var fields = string.Join(", ", columns.Select(c => $"[{c}]"));
        var sql = query.ToString()!.Replace("[Data]", fields);

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var conn = new SqlConnection(m_connectionString);
                var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                // CommandBehavior.CloseConnection ensures connection closes when reader is disposed
                return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            });

        if (pr.FinalException is not null)
        {
            System.Diagnostics.Debug.WriteLine($"SQL Error for {typeof(T).Name}: {sql}");
            Console.WriteLine($"SQL Error for {typeof(T).Name}: {sql}");
            throw new InvalidOperationException($"SQL Error for {typeof(T).Name}: {sql}", pr.FinalException);
        }

        return pr.Result;
    }
}
