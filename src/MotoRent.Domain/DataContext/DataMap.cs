using System.Linq.Expressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// A dictionary-like object containing specific columns from an entity query.
/// Used for performance-optimized queries where full entity deserialization isn't needed.
/// </summary>
/// <typeparam name="TEntity">The entity type this map was created from</typeparam>
public class DataMap<TEntity> : Dictionary<string, object?> where TEntity : Entity
{
    /// <summary>
    /// Gets a value from the map using a strongly-typed expression selector.
    /// Handles type conversions (DateTime → DateOnly, DBNull → default).
    /// </summary>
    /// <typeparam name="TResult">The expected return type</typeparam>
    /// <param name="selector">Expression selecting the property (e.g., t => t.ShopId)</param>
    /// <returns>The value converted to TResult, or default if null/missing</returns>
    public TResult? GetValue<TResult>(Expression<Func<TEntity, TResult>> selector)
    {
        var memberName = GetMemberName(selector);
        if (string.IsNullOrEmpty(memberName))
            return default;

        if (!this.TryGetValue(memberName, out var value))
            return default;

        return ConvertToType<TResult>(value);
    }

    /// <summary>
    /// Gets a value from the map by column name.
    /// </summary>
    public TResult? GetValue<TResult>(string columnName)
    {
        if (!this.TryGetValue(columnName, out var value))
            return default;

        return ConvertToType<TResult>(value);
    }

    private static string? GetMemberName<TResult>(Expression<Func<TEntity, TResult>> selector)
    {
        if (selector.Body is MemberExpression memberExpr)
            return memberExpr.Member.Name;

        // Handle Nullable<T> properties wrapped in Convert
        if (selector.Body is UnaryExpression { Operand: MemberExpression unaryMember })
            return unaryMember.Member.Name;

        return null;
    }

    private static TResult? ConvertToType<TResult>(object? value)
    {
        if (value == null || value == DBNull.Value)
            return default;

        var targetType = typeof(TResult);
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle enums
        if (underlyingType.IsEnum)
        {
            if (Enum.TryParse(underlyingType, value.ToString(), out var enumValue))
                return (TResult)enumValue!;
            return default;
        }

        // Handle DateOnly from DateTime
        if (underlyingType == typeof(DateOnly) && value is DateTime dt)
            return (TResult)(object)DateOnly.FromDateTime(dt);

        // Handle TimeOnly from TimeSpan
        if (underlyingType == typeof(TimeOnly) && value is TimeSpan ts)
            return (TResult)(object)TimeOnly.FromTimeSpan(ts);

        // Handle DateTimeOffset
        if (underlyingType == typeof(DateTimeOffset) && value is DateTime dateTime)
            return (TResult)(object)new DateTimeOffset(dateTime);

        // Standard conversion
        try
        {
            return (TResult)Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            return default;
        }
    }
}
