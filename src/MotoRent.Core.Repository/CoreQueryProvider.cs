using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace MotoRent.Core.Repository;

internal static class QueryTypeSystem
{
    private static Type? FindIEnumerable(Type seqType)
    {
        if (seqType == typeof(string))
            return null;
        if (seqType.IsArray)
            return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType()!);
        if (seqType.IsGenericType)
        {
            foreach (Type arg in seqType.GetGenericArguments())
            {
                Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                if (ienum.IsAssignableFrom(seqType))
                {
                    return ienum;
                }
            }
        }
        var ifaces = seqType.GetInterfaces();
        if (ifaces.Length > 0)
        {
            foreach (Type iface in ifaces)
            {
                Type? ienum = FindIEnumerable(iface);
                if (ienum != null) return ienum;
            }
        }
        if (seqType.BaseType != null && seqType.BaseType != typeof(object))
        {
            return FindIEnumerable(seqType.BaseType);
        }
        return null;
    }

    internal static Type GetSequenceType(Type elementType)
    {
        return typeof(IEnumerable<>).MakeGenericType(elementType);
    }

    internal static Type GetElementType(Type seqType)
    {
        Type? ienum = FindIEnumerable(seqType);
        if (ienum == null) return seqType;
        return ienum.GetGenericArguments()[0];
    }
}

/// <summary>
/// A basic abstract LINQ query provider
/// </summary>
public abstract class CoreQueryProvider : IQueryProvider
{
    IQueryable<TS> IQueryProvider.CreateQuery<TS>(Expression expression)
    {
        return new CoreQuery<TS>(this, expression);
    }

    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        var elementType = QueryTypeSystem.GetElementType(expression.Type);
        try
        {
            return (IQueryable)Activator.CreateInstance(typeof(CoreQuery<>).MakeGenericType(elementType), this, expression)!;
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException ?? tie;
        }
    }

    TS IQueryProvider.Execute<TS>(Expression expression)
    {
        return (TS)this.Execute(expression)!;
    }

    object? IQueryProvider.Execute(Expression expression)
    {
        return this.Execute(expression);
    }

    public abstract string GetQueryText(Expression expression);
    public abstract object? Execute(Expression expression);
}

/// <summary>
/// A default implementation of IQueryable for use with QueryProvider
/// </summary>
public class CoreQuery<T> : IOrderedQueryable<T>
{
    private readonly CoreQueryProvider m_provider;
    private readonly Expression m_expression;

    public CoreQuery(CoreQueryProvider provider)
    {
        this.m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.m_expression = Expression.Constant(this);
    }

    public CoreQuery(CoreQueryProvider provider, Expression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }
        if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
        {
            throw new ArgumentOutOfRangeException(nameof(expression));
        }
        this.m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.m_expression = expression;
    }

    Expression IQueryable.Expression => this.m_expression;

    Type IQueryable.ElementType => typeof(T);

    IQueryProvider IQueryable.Provider => this.m_provider;

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)this.m_provider.Execute(this.m_expression)!).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)this.m_provider.Execute(this.m_expression)!).GetEnumerator();
    }

    public override string ToString()
    {
        return this.m_provider.GetQueryText(this.m_expression);
    }
}
