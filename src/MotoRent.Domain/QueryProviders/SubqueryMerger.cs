using System.Linq.Expressions;

namespace MotoRent.Domain.QueryProviders;

/// <summary>
/// Merges WHERE clauses from nested subqueries into a single flat query.
/// Transforms:
///   SELECT * FROM (SELECT * FROM T WHERE a) WHERE b
/// Into:
///   SELECT * FROM T WHERE (a) AND (b)
/// </summary>
public class SubqueryMerger : DbExpressionVisitor
{
    public Expression Merge(Expression expression)
    {
        return this.Visit(expression)!;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        // First, recursively process any nested selects
        select = (SelectExpression)base.VisitSelect(select);

        // Check if we can merge: outer SELECT has FROM as another SELECT
        if (select.From is SelectExpression innerSelect)
        {
            // Check if we can merge the inner select into the outer
            if (CanMerge(select, innerSelect))
            {
                return MergeSelects(select, innerSelect);
            }
        }

        return select;
    }

    private static bool CanMerge(SelectExpression outer, SelectExpression inner)
    {
        // Can merge if:
        // 1. Inner has no ORDER BY (or outer has none and can inherit)
        // 2. Neither uses DISTINCT, TOP, aggregations, etc.
        // 3. Outer is just selecting * from inner (no column transformations)

        // Check outer is simple "SELECT *" from subquery
        var outerIsSimpleSelect = IsSimpleSelectStar(outer);

        // Check inner has no complex clauses that prevent merging
        var innerCanBeMerged = inner.OrderBy == null || inner.OrderBy.Count == 0;

        return outerIsSimpleSelect && innerCanBeMerged;
    }

    private static bool IsSimpleSelectStar(SelectExpression select)
    {
        // A simple select is one that just passes through columns from its source
        // (i.e., SELECT * FROM ...)
        if (select.Columns.Count == 0)
            return true;

        // Check if all columns are just pass-through from the inner select
        foreach (var col in select.Columns)
        {
            if (col.Expression is not ColumnExpression)
                return false;
        }

        return true;
    }

    private static SelectExpression MergeSelects(SelectExpression outer, SelectExpression inner)
    {
        // Merge WHERE clauses
        Expression? mergedWhere = null;
        if (outer.Where != null && inner.Where != null)
        {
            // Combine: (inner.Where) AND (outer.Where)
            mergedWhere = Expression.AndAlso(inner.Where, outer.Where);
        }
        else if (outer.Where != null)
        {
            mergedWhere = outer.Where;
        }
        else if (inner.Where != null)
        {
            mergedWhere = inner.Where;
        }

        // Use outer's ORDER BY if it has one, otherwise inner's
        var mergedOrderBy = outer.OrderBy is { Count: > 0 }
            ? outer.OrderBy
            : inner.OrderBy;

        // Determine which columns to use
        // If inner is from a table, use inner's columns; otherwise keep recursing
        var mergedColumns = inner.Columns.Count > 0 ? inner.Columns : outer.Columns;

        // Create merged select with inner's FROM source
        return new SelectExpression(
            outer.Type,
            outer.Alias,
            mergedColumns,
            inner.From,
            mergedWhere!,
            mergedOrderBy);
    }
}
