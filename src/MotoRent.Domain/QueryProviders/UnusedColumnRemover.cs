using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace MotoRent.Domain.QueryProviders;

/// <summary>
/// Removes column declarations in SelectExpression's that are not referenced
/// </summary>
public class UnusedColumnRemover : DbExpressionVisitor
{
    Dictionary<string, HashSet<string>> m_allColumnsUsed = new();

    public Expression Remove(Expression expression)
    {
        return this.Visit(expression)!;
    }

    protected override Expression VisitColumn(ColumnExpression column)
    {
        MarkColumnAsUsed(column.Alias, column.Name);
        return column;
    }

    void MarkColumnAsUsed(string alias, string name)
    {
        if (!m_allColumnsUsed.TryGetValue(alias, out var columns))
        {
            columns = new HashSet<string>();
            m_allColumnsUsed.Add(alias, columns);
        }
        columns.Add(name);
    }

    bool IsColumnUsed(string alias, string name)
    {
        if (m_allColumnsUsed.TryGetValue(alias, out var columnsUsed))
        {
            if (columnsUsed != null)
            {
                return columnsUsed.Contains(name);
            }
        }
        return false;
    }

    void ClearColumnsUsed(string alias)
    {
        this.m_allColumnsUsed[alias] = new HashSet<string>();
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        // visit column expressions first
        ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;
        var wasUsed = this.m_allColumnsUsed;
        this.m_allColumnsUsed = new Dictionary<string, HashSet<string>>();
        // first mark all columns that must be kept
        foreach (ColumnDeclaration c in columns)
        {
            MarkColumnAsUsed(select.Alias, c.Name);
        }
        List<ColumnDeclaration>? alternate = null;
        for (int i = 0, n = columns.Count; i < n; i++)
        {
            ColumnDeclaration decl = columns[i];
            if (IsColumnUsed(select.Alias, decl.Name))
            {
                Expression expr = this.Visit(decl.Expression)!;
                if (expr != decl.Expression)
                {
                    decl = new ColumnDeclaration(decl.Name, expr);
                }
            }
            else
            {
                decl = null!;
            }
            if (decl != columns[i] && alternate == null)
            {
                alternate = new List<ColumnDeclaration>();
                for (int j = 0; j < i; j++)
                {
                    alternate.Add(columns[j]);
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
        ReadOnlyCollection<OrderExpression>? orderings = this.VisitOrderBy(select.OrderBy);
        Expression? where = this.Visit(select.Where);
        Expression from = this.VisitSource(select.From);
        ClearColumnsUsed(select.Alias);
        this.m_allColumnsUsed = wasUsed;
        if (columns != select.Columns || orderings != select.OrderBy || @where != select.Where || from != select.From)
        {
            select = new SelectExpression(select.Type, select.Alias, columns, from, @where!, orderings!);
        }
        return select;
    }
}
