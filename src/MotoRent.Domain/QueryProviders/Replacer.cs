using System.Linq.Expressions;

namespace MotoRent.Domain.QueryProviders;

/// <summary>
/// Replaces expression nodes in a tree
/// </summary>
public class Replacer : DbExpressionVisitor
{
    readonly Expression m_searchFor;
    readonly Expression m_replaceWith;

    private Replacer(Expression searchFor, Expression replaceWith)
    {
        this.m_searchFor = searchFor;
        this.m_replaceWith = replaceWith;
    }

    public static Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
    {
        return new Replacer(searchFor, replaceWith).Visit(expression)!;
    }

    protected override Expression? Visit(Expression? exp)
    {
        if (exp == this.m_searchFor)
        {
            return this.m_replaceWith;
        }
        return base.Visit(exp);
    }
}
