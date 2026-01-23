using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace MotoRent.Domain.QueryProviders;

public class SubqueryRemover : DbExpressionVisitor
{
    readonly HashSet<SelectExpression> m_selectsToRemove;
    readonly Dictionary<string, Dictionary<string, Expression>> m_map;

    private SubqueryRemover(IEnumerable<SelectExpression> selectsToRemove)
    {
        this.m_selectsToRemove = new HashSet<SelectExpression>(selectsToRemove);
        this.m_map = this.m_selectsToRemove.ToDictionary(d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));
    }

    internal static SelectExpression Remove(SelectExpression outerSelect, IEnumerable<SelectExpression> selectsToRemove)
    {
        return (SelectExpression)new SubqueryRemover(selectsToRemove).Visit(outerSelect)!;
    }

    internal static ProjectionExpression Remove(ProjectionExpression projection, IEnumerable<SelectExpression> selectsToRemove)
    {
        return (ProjectionExpression)new SubqueryRemover(selectsToRemove).Visit(projection)!;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        if (this.m_selectsToRemove.Contains(select))
        {
            return this.Visit(select.From)!;
        }
        return base.VisitSelect(select);
    }

    protected override Expression? VisitColumn(ColumnExpression column)
    {
        if (this.m_map.TryGetValue(column.Alias, out var nameMap))
        {
            if (nameMap.TryGetValue(column.Name, out var expr))
            {
                return this.Visit(expr);
            }
            throw new Exception("Reference to undefined column");
        }
        return column;
    }
}
