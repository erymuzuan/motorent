using System.Linq.Expressions;

namespace MotoRent.Domain.QueryProviders;

/// <summary>
/// Removes select expressions that don't add any additional semantic value
/// </summary>
public class RedundantSubqueryRemover : DbExpressionVisitor
{
    public Expression Remove(Expression expression)
    {
        return this.Visit(expression)!;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        select = (SelectExpression)base.VisitSelect(select);

        // first remove all purely redundant subqueries
        List<SelectExpression> redundant = RedundantSubqueryGatherer.Gather(select.From);
        if (redundant != null)
        {
            select = SubqueryRemover.Remove(select, redundant);
        }

        return select;
    }

    protected override Expression VisitProjection(ProjectionExpression proj)
    {
        proj = (ProjectionExpression)base.VisitProjection(proj);
        if (proj.Source.From is SelectExpression)
        {
            List<SelectExpression> redundant = RedundantSubqueryGatherer.Gather(proj.Source);
            if (redundant != null)
            {
                proj = SubqueryRemover.Remove(proj, redundant);
            }
        }
        return proj;
    }

    static bool IsRedudantSubquery(SelectExpression select)
    {
        return (select.From is SelectExpression)
            && select.Where == null
            && (select.OrderBy == null || select.OrderBy.Count == 0)
            && !IsDistinct(select)
            && !IsAggregation(select);
    }

    static bool IsDistinct(SelectExpression select)
    {
        return false; // not tracking distinct yet
    }

    static bool IsAggregation(SelectExpression select)
    {
        return false; // not tracking aggregation yet
    }

    class RedundantSubqueryGatherer : DbExpressionVisitor
    {
        List<SelectExpression>? m_redundant;

        private RedundantSubqueryGatherer()
        {
        }

        internal static List<SelectExpression> Gather(Expression source)
        {
            var gatherer = new RedundantSubqueryGatherer();
            gatherer.Visit(source);
            return gatherer.m_redundant!;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            if (IsRedudantSubquery(select))
            {
                if (this.m_redundant == null)
                {
                    this.m_redundant = new List<SelectExpression>();
                }
                this.m_redundant.Add(select);
                return this.Visit(select.From)!;
            }
            return select;
        }
    }
}
