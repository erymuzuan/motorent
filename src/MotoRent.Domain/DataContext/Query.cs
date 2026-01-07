using System.Collections;
using System.Linq.Expressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

public class Query<T> : IQueryable<T>, IOrderedQueryable<T> where T : Entity
{
    private readonly QueryProvider m_provider;
    private readonly Expression m_expression;
    private List<(string Column, bool Descending)> m_orderBy = [];
    private List<Expression<Func<T, bool>>> m_predicates = [];

    public Query(QueryProvider provider)
    {
        m_provider = provider;
        m_expression = Expression.Constant(this);
    }

    public Query(QueryProvider provider, Expression expression)
    {
        m_provider = provider;
        m_expression = expression;
    }

    internal QueryProvider Provider => m_provider;
    internal List<(string Column, bool Descending)> OrderByColumns => m_orderBy;
    internal List<Expression<Func<T, bool>>> Predicates => m_predicates;

    public Type ElementType => typeof(T);
    public Expression Expression => m_expression;
    IQueryProvider IQueryable.Provider => new QueryableProvider<T>(this);

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotSupportedException("Use LoadAsync to execute query");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Query<T> Where(Expression<Func<T, bool>> predicate)
    {
        var newQuery = Clone();
        newQuery.m_predicates.Add(predicate);
        return newQuery;
    }

    public Query<T> OrderBy(Expression<Func<T, object>> keySelector)
    {
        var newQuery = Clone();
        var column = GetColumnName(keySelector);
        newQuery.m_orderBy.Add((column, false));
        return newQuery;
    }

    public Query<T> OrderByDescending(Expression<Func<T, object>> keySelector)
    {
        var newQuery = Clone();
        var column = GetColumnName(keySelector);
        newQuery.m_orderBy.Add((column, true));
        return newQuery;
    }

    public Query<T> ThenBy(Expression<Func<T, object>> keySelector)
    {
        var column = GetColumnName(keySelector);
        m_orderBy.Add((column, false));
        return this;
    }

    public Query<T> ThenByDescending(Expression<Func<T, object>> keySelector)
    {
        var column = GetColumnName(keySelector);
        m_orderBy.Add((column, true));
        return this;
    }

    private Query<T> Clone()
    {
        var clone = new Query<T>(m_provider, m_expression);
        clone.m_orderBy = new List<(string, bool)>(m_orderBy);
        clone.m_predicates = new List<Expression<Func<T, bool>>>(m_predicates);
        return clone;
    }

    private static string GetColumnName(Expression<Func<T, object>> expression)
    {
        if (expression.Body is UnaryExpression unary)
        {
            if (unary.Operand is MemberExpression member)
                return member.Member.Name;
        }
        if (expression.Body is MemberExpression memberExpr)
            return memberExpr.Member.Name;

        throw new ArgumentException("Invalid column expression");
    }
}

internal class QueryableProvider<T> : IQueryProvider where T : Entity
{
    private readonly Query<T> m_query;

    public QueryableProvider(Query<T> query)
    {
        m_query = query;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new Query<T>(m_query.Provider, expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(T))
            throw new NotSupportedException("Cross-type queries not supported");

        return (IQueryable<TElement>)new Query<T>(m_query.Provider, expression);
    }

    public object? Execute(Expression expression)
    {
        throw new NotSupportedException("Use LoadAsync to execute query");
    }

    public TResult Execute<TResult>(Expression expression)
    {
        throw new NotSupportedException("Use LoadAsync to execute query");
    }
}
