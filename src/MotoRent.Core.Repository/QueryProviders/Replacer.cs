using System.Linq.Expressions;

namespace MotoRent.Core.Repository.QueryProviders;

/// <summary>
/// A visitor that replaces references to one specific instance of a node with another
/// </summary>
internal class Replacer : DbExpressionVisitor {
    Expression? m_searchFor;
    Expression? m_replaceWith;

    internal Expression Replace(Expression expression, Expression searchFor, Expression replaceWith) {
        this.m_searchFor = searchFor;
        this.m_replaceWith = replaceWith;
        return this.Visit(expression)!;
    }

    public override Expression? Visit(Expression? exp) {
        if (exp == this.m_searchFor) {
            return this.m_replaceWith;
        }
        return base.Visit(exp);
    }
}
