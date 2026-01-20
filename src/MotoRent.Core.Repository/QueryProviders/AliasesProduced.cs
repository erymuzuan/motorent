using System.Linq.Expressions;

namespace MotoRent.Core.Repository.QueryProviders;

/// <summary>
///  returns the set of all aliases produced by a query source
/// </summary>
public class AliasesProduced : DbExpressionVisitor
{
    HashSet<string> m_aliases = new();

    public HashSet<string> Gather(Expression source)
    {
        this.m_aliases = new HashSet<string>();
        this.Visit(source);
        return this.m_aliases;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        this.m_aliases.Add(select.Alias);
        return select;
    }

    protected override Expression VisitTable(TableExpression table)
    {
        this.m_aliases.Add(table.Alias);
        return table;
    }
}
