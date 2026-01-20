using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace MotoRent.Core.Repository.QueryProviders;

/// <summary>
/// Move order-bys to the outermost select
/// </summary>
public class OrderByRewriter : DbExpressionVisitor
{
    IEnumerable<OrderExpression>? m_gatheredOrderings;
    bool m_isOuterMostSelect;

    public Expression Rewrite(Expression expression)
    {
        this.m_isOuterMostSelect = true;
        return this.Visit(expression)!;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        bool saveIsOuterMostSelect = this.m_isOuterMostSelect;
        try
        {
            this.m_isOuterMostSelect = false;
            select = (SelectExpression)base.VisitSelect(select);
            bool hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
            if (hasOrderBy)
            {
                this.PrependOrderings(select.OrderBy!);
            }
            bool canHaveOrderBy = saveIsOuterMostSelect;
            bool canPassOnOrderings = !saveIsOuterMostSelect;
            IEnumerable<OrderExpression>? orderings = (canHaveOrderBy) ? this.m_gatheredOrderings : null;
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;
            if (this.m_gatheredOrderings != null)
            {
                if (canPassOnOrderings)
                {
                    HashSet<string> producedAliases = new AliasesProduced().Gather(select.From);
                    // reproject order expressions using this select's alias so the outer select will have properly formed expressions
                    BindResult project = this.RebindOrderings(this.m_gatheredOrderings, select.Alias, producedAliases, select.Columns);
                    this.m_gatheredOrderings = project.Orderings;
                    columns = project.Columns;
                }
                else
                {
                    this.m_gatheredOrderings = null;
                }
            }
            if (orderings != select.OrderBy || columns != select.Columns)
            {
                select = new SelectExpression(select.Type, select.Alias, columns, select.From, select.Where, orderings);
            }
            return select;
        }
        finally
        {
            this.m_isOuterMostSelect = saveIsOuterMostSelect;
        }
    }

    protected override Expression VisitJoin(JoinExpression join)
    {
        // make sure order by expressions lifted up from the left side are not lost
        // when visiting the right side
        Expression left = this.VisitSource(join.Left);
        IEnumerable<OrderExpression>? leftOrders = this.m_gatheredOrderings;
        this.m_gatheredOrderings = null; // start on the right with a clean slate
        Expression right = this.VisitSource(join.Right);
        this.PrependOrderings(leftOrders);
        Expression? condition = this.Visit(join.Condition);
        if (left != join.Left || right != join.Right || condition != join.Condition)
        {
            return new JoinExpression(join.Type, join.Join, left, right, condition);
        }
        return join;
    }

    /// <summary>
    /// Add a sequence of order expressions to an accumulated list, prepending so as
    /// to give precedence to the new expressions over any previous expressions
    /// </summary>
    /// <param name="newOrderings"></param>
    protected void PrependOrderings(IEnumerable<OrderExpression>? newOrderings)
    {
        if (newOrderings != null)
        {
            if (this.m_gatheredOrderings == null)
            {
                this.m_gatheredOrderings = newOrderings;
            }
            else
            {
                var list = this.m_gatheredOrderings as List<OrderExpression>;
                if (list == null)
                {
                    this.m_gatheredOrderings = list = new List<OrderExpression>(this.m_gatheredOrderings);
                }
                list.InsertRange(0, newOrderings);
            }
        }
    }

    protected class BindResult
    {
        public BindResult(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orderings)
        {
            this.Columns = columns as ReadOnlyCollection<ColumnDeclaration> ??
                           new List<ColumnDeclaration>(columns).AsReadOnly();
            this.Orderings = orderings as ReadOnlyCollection<OrderExpression>;
            if (this.Orderings == null)
            {
                this.Orderings = new List<OrderExpression>(orderings).AsReadOnly();
            }
        }
        public ReadOnlyCollection<ColumnDeclaration> Columns { get; }

        public ReadOnlyCollection<OrderExpression> Orderings { get; }
    }

    /// <summary>
    /// Rebind order expressions to reference a new alias and add to column declarations if necessary
    /// </summary>
    protected virtual BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, string alias, HashSet<string>? existingAliases, IEnumerable<ColumnDeclaration> existingColumns)
    {
        List<ColumnDeclaration>? newColumns = null;
        var newOrderings = new List<OrderExpression>();
        foreach (OrderExpression ordering in orderings)
        {
            Expression expr = ordering.Expression;
            var column = expr as ColumnExpression;
            if (column == null || (existingAliases != null && existingAliases.Contains(column.Alias)))
            {
                // check to see if a declared column already contains a similar expression
                int iOrdinal = 0;
                foreach (ColumnDeclaration decl in existingColumns)
                {
                    var declColumn = decl.Expression as ColumnExpression;
                    if (decl.Expression == ordering.Expression ||
                        (column != null && declColumn != null && column.Alias == declColumn.Alias && column.Name == declColumn.Name))
                    {
                        // found it, so make a reference to this column
                        expr = new ColumnExpression(column!.Type, alias, decl.Name, iOrdinal);
                        break;
                    }
                    iOrdinal++;
                }
                // if not already projected, add a new column declaration for it
                if (expr == ordering.Expression)
                {
                    if (newColumns == null)
                    {
                        newColumns = new List<ColumnDeclaration>(existingColumns);
                        existingColumns = newColumns;
                    }
                    string colName = column != null ? column.Name : "c" + iOrdinal;
                    newColumns.Add(new ColumnDeclaration(colName, ordering.Expression));
                    expr = new ColumnExpression(expr.Type, alias, colName, iOrdinal);
                }
                newOrderings.Add(new OrderExpression(ordering.OrderType, expr));
            }
        }
        return new BindResult(existingColumns, newOrderings);
    }
}
