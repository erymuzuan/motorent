using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace MotoRent.Domain.QueryProviders;

/// <summary>
/// Compiles expressions that construct result objects from IDataRecord
/// </summary>
public static class ProjectionBuilder
{
    public static LambdaExpression Build(Expression expression, string alias)
    {
        return new ProjectionBuilder2(alias).Build(expression);
    }
}

internal class ProjectionBuilder2 : DbExpressionVisitor
{
    readonly ParameterExpression m_row;
    readonly string m_alias;

    internal ProjectionBuilder2(string alias)
    {
        this.m_alias = alias;
        this.m_row = Expression.Parameter(typeof(IDataRecord), "row");
    }

    internal LambdaExpression Build(Expression expression)
    {
        return Expression.Lambda(this.Visit(expression)!, this.m_row);
    }

    protected override Expression? VisitColumn(ColumnExpression column)
    {
        if (column.Alias == this.m_alias)
        {
            return Expression.Convert(
                Expression.Call(this.m_row, typeof(IDataRecord).GetMethod("get_Item", new Type[] { typeof(int) })!,
                    Expression.Constant(column.Ordinal)),
                column.Type
            );
        }
        return column;
    }
}

public class ProjectionRow
{
    readonly IDataRecord m_record;
    static readonly MethodInfo s_miGetValue;

    static ProjectionRow()
    {
        s_miGetValue = typeof(ProjectionRow).GetMethod("GetValue")!;
    }

    internal ProjectionRow(IDataRecord record)
    {
        this.m_record = record;
    }

    public T GetValue<T>(int ordinal)
    {
        if (this.m_record.IsDBNull(ordinal))
        {
            return default!;
        }
        return (T)this.m_record.GetValue(ordinal);
    }

    internal static MethodInfo GetValueMethodInfo()
    {
        return s_miGetValue;
    }
}
