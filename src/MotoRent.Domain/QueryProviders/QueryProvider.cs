using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace MotoRent.Domain.QueryProviders;

/// <summary>
/// A basic abstract LINQ query provider
/// </summary>
public abstract class QueryProvider : IQueryProvider {
    IQueryable<TS> IQueryProvider.CreateQuery<TS>(Expression expression) {
        return new Query<TS>(this, expression);
    }

    IQueryable IQueryProvider.CreateQuery(Expression expression) {
        Type elementType = TypeSystem.GetElementType(expression.Type);
        try {
            return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression })!;
        }
        catch (TargetInvocationException tie) {
            throw tie.InnerException!;
        }
    }

    TS IQueryProvider.Execute<TS>(Expression expression) {
        return (TS)this.Execute(expression)!;
    }

    object? IQueryProvider.Execute(Expression expression) {
        return this.Execute(expression);
    }

    public abstract string GetQueryText(Expression expression);
    public abstract object? Execute(Expression expression);
}

/// <summary>
/// A default implementation of IQueryable for use with QueryProvider
/// </summary>
public class Query<T> : IOrderedQueryable<T> {
    readonly QueryProvider m_provider;
    readonly Expression m_expression;

    public Query(QueryProvider provider) {
        this.m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.m_expression = Expression.Constant(this);
    }

    public Query(QueryProvider provider, Expression expression) {
        if (expression == null) {
            throw new ArgumentNullException(nameof(expression));
        }
        if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type)) {
            throw new ArgumentOutOfRangeException(nameof(expression));
        }
        this.m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.m_expression = expression;
    }

    Expression IQueryable.Expression => this.m_expression;

    Type IQueryable.ElementType => typeof(T);

    IQueryProvider IQueryable.Provider => this.m_provider;

    public IEnumerator<T> GetEnumerator() {
        return ((IEnumerable<T>)this.m_provider.Execute(this.m_expression)!).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)this.m_provider.Execute(this.m_expression)!).GetEnumerator();
    }

    public override string ToString() {
        return this.m_provider.GetQueryText(this.m_expression);
    }
}
