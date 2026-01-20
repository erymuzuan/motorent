using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace MotoRent.Core.Repository.QueryProviders;

public class UnusedColumnRemover : DbExpressionVisitor
{
    Dictionary<string, HashSet<string>> m_allColumnsUsed = new();

    public Expression Remove(Expression expression)
    {
        this.m_allColumnsUsed = new Dictionary<string, HashSet<string>>();
        return this.Visit(expression)!;
    }

    protected override Expression VisitColumn(ColumnExpression column)
    {
        if (!this.m_allColumnsUsed.TryGetValue(column.Alias, out HashSet<string>? columns))
        {
            columns = new HashSet<string>();
            this.m_allColumnsUsed.Add(column.Alias, columns);
        }
        columns.Add(column.Name);
        return column;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        // visit column projection first
        ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;

        if (this.m_allColumnsUsed.TryGetValue(select.Alias, out HashSet<string>? columnsUsed))
        {
            List<ColumnDeclaration>? alternate = null;
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                ColumnDeclaration? decl = select.Columns[i];
                if (!columnsUsed.Contains(decl.Name))
                {
                    decl = null;  // null means it gets omitted
                }
                else
                {
                    Expression expr = this.Visit(decl.Expression)!;
                    if (expr != decl.Expression)
                    {
                        decl = new ColumnDeclaration(decl.Name, decl.Expression);
                    }
                }
                if (decl != select.Columns[i] && alternate == null)
                {
                    alternate = new List<ColumnDeclaration>();
                    for (int j = 0; j < i; j++)
                    {
                        alternate.Add(select.Columns[j]);
                    }
                }
                if (decl != null && alternate != null)
                {
                    alternate.Add(decl);
                }
            }
            if (alternate != null)
            {
                columns = alternate.AsReadOnly();
            }
        }

        ReadOnlyCollection<OrderExpression>? orderbys = this.VisitOrderBy(select.OrderBy);
        Expression? where = this.Visit(select.Where);
        Expression from = this.Visit(select.From)!;

        if (columns != select.Columns || orderbys != select.OrderBy || where != select.Where || from != select.From)
        {
            return new SelectExpression(select.Type, select.Alias, columns, from, where, orderbys);
        }

        return select;
    }

    protected override Expression VisitProjection(ProjectionExpression projection)
    {
        // visit mapping in reverse order
        Expression projector = this.Visit(projection.Projector)!;
        var source = (SelectExpression)this.Visit(projection.Source)!;
        if (projector != projection.Projector || source != projection.Source)
        {
            return new ProjectionExpression(source, projector);
        }
        return projection;
    }

    protected override Expression VisitJoin(JoinExpression join)
    {
        // visit join in reverse order
        Expression? condition = this.Visit(join.Condition);
        Expression right = this.VisitSource(join.Right);
        Expression left = this.VisitSource(join.Left);
        if (left != join.Left || right != join.Right || condition != join.Condition)
        {
            return new JoinExpression(join.Type, join.Join, left, right, condition);
        }
        return join;
    }
}
