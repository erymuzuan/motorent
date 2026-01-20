using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace MotoRent.Core.Repository.QueryProviders;

/// <summary>
/// A ProjectionRow is an abstract over a row based data source
/// </summary>
public abstract class ProjectionRow {
    public abstract object GetValue(int index);
    public abstract IEnumerable<TE> ExecuteSubQuery<TE>(LambdaExpression query);
}

/// <summary>
/// ProjectionBuilder is a visitor that converts an projector expression
/// that constructs result objects out of ColumnExpressions into an actual
/// LambdaExpression that constructs result objects out of accessing fields
/// of a ProjectionRow
/// </summary>
public class ProjectionBuilder : DbExpressionVisitor {
    ParameterExpression? m_row;
    string m_rowAlias = string.Empty;
    static MethodInfo? s_miGetValue;
    static MethodInfo? s_miExecuteSubQuery;

    public ProjectionBuilder() {
        if (s_miGetValue == null) {
            s_miGetValue = typeof(ProjectionRow).GetMethod("GetValue");
            s_miExecuteSubQuery = typeof(ProjectionRow).GetMethod("ExecuteSubQuery");
        }
    }

    public LambdaExpression Build(Expression expression, string alias) {
        this.m_row = Expression.Parameter(typeof(ProjectionRow), "row");
        this.m_rowAlias = alias;
        Expression body = this.Visit(expression)!;
        return Expression.Lambda(body, this.m_row);
    }

    protected override Expression VisitColumn(ColumnExpression column) {
        if (column.Alias == this.m_rowAlias) {
            return Expression.Convert(Expression.Call(this.m_row!, s_miGetValue!, Expression.Constant(column.Ordinal)), column.Type);
        }
        return column;
    }

    protected override Expression VisitProjection(ProjectionExpression proj) {
        LambdaExpression subQuery = Expression.Lambda(base.VisitProjection(proj), this.m_row!);
        Type elementType = TypeSystem.GetElementType(subQuery.Body.Type);
        MethodInfo mi = s_miExecuteSubQuery!.MakeGenericMethod(elementType);
        return Expression.Convert(
            Expression.Call(this.m_row!, mi, Expression.Constant(subQuery)),
            proj.Type
        );
    }
}


/// <summary>
/// ProjectionReader is an implemention of IEnumerable that converts data from DbDataReader into
/// objects via a projector function,
/// </summary>
/// <typeparam name="T"></typeparam>
public class ProjectionReader<T> : IEnumerable<T>, IEnumerable {
    Enumerator? m_enumerator;

    public ProjectionReader(Func<ProjectionRow, T> projector, IQueryProvider provider) {
        this.m_enumerator = new Enumerator(projector, provider);
    }

    public IEnumerator<T> GetEnumerator() {
        Enumerator? e = this.m_enumerator;
        if (e == null) {
            throw new InvalidOperationException("Cannot enumerate more than once");
        }
        this.m_enumerator = null;
        return e;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }

    class Enumerator : ProjectionRow, IEnumerator<T> {
        Func<ProjectionRow, T> m_projector;
        readonly IQueryProvider m_provider;

        public Enumerator(Func<ProjectionRow, T> projector, IQueryProvider provider) {
            this.m_projector = projector;
            this.m_provider = provider;
        }

        public override object GetValue(int index) {
            throw new IndexOutOfRangeException();
        }

        public override IEnumerable<TE> ExecuteSubQuery<TE>(LambdaExpression query) {
            var projection = (ProjectionExpression) new Replacer().Replace(query.Body, query.Parameters[0], Expression.Constant(this));
            projection = (ProjectionExpression) Evaluator.PartialEval(projection, CanEvaluateLocally);
            var result = (IEnumerable<TE>)this.m_provider.Execute(projection)!;
            var list = new List<TE>(result);
            if (typeof(IQueryable<TE>).IsAssignableFrom(query.Body.Type)) {
                return list.AsQueryable();
            }
            return list;
        }

        private static bool CanEvaluateLocally(Expression expression) {
            if (expression.NodeType == ExpressionType.Parameter ||
                expression.NodeType.IsDbExpression()) {
                return false;
            }
            return true;
        }

        public T Current { get; private set; } = default!;

        object? IEnumerator.Current => this.Current;

        public bool MoveNext() {
            return false;
        }

        public void Reset() {
        }

        public void Dispose() {
        }
    }
}
