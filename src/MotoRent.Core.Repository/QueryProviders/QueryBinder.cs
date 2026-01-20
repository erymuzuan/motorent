using System.Linq.Expressions;
using System.Reflection;

namespace MotoRent.Core.Repository.QueryProviders;

/// <summary>
/// QueryBinder is a visitor that converts method calls to LINQ operations into
/// custom DbExpression nodes and references to class members into references to columns
/// </summary>
public class QueryBinder : ExpressionVisitor
{
    readonly ColumnProjector m_columnProjector;
    Dictionary<ParameterExpression, Expression> m_map = new();
    int m_aliasCount;
    readonly IQueryProvider m_provider;

    public QueryBinder(IQueryProvider provider)
    {
        this.m_provider = provider;
        this.m_columnProjector = new ColumnProjector(this.CanBeColumn);
    }

    private bool CanBeColumn(Expression expression)
    {
        return expression.NodeType == (ExpressionType)DbExpressionType.Column;
    }

    public Expression Bind(Expression expression)
    {
        this.m_map = new Dictionary<ParameterExpression, Expression>();
        return this.Visit(expression)!;
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }
        return e;
    }

    private string GetNextAlias()
    {
        return "t" + (m_aliasCount++);
    }

    private ProjectedColumns ProjectColumns(Expression expression, string newAlias, params string[] existingAliases)
    {
        return this.m_columnProjector.ProjectColumns(expression, newAlias, existingAliases);
    }

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        if (m.Method.DeclaringType == typeof(Queryable) ||
            m.Method.DeclaringType == typeof(Enumerable))
        {
            switch (m.Method.Name)
            {
                case "SingleOrDefault":
                case "FirstOrDefault":
                case "Single":
                case "First":
                case "Where":
                case "Count":
                    return this.BindWhere(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                case "Contains":
                    var agr = StripQuotes(m.Arguments[1]);
                    var le = agr as LambdaExpression;
                    if (null != le)
                        return this.BindWhere(m.Type, m.Arguments[0], le);
                    break;
                case "Select":
                    return this.BindSelect(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                case "SelectMany":
                    if (m.Arguments.Count == 2)
                    {
                        return this.BindSelectMany(
                            m.Type, m.Arguments[0],
                            (LambdaExpression)StripQuotes(m.Arguments[1]),
                            null
                        );
                    }
                    if (m.Arguments.Count == 3)
                    {
                        return this.BindSelectMany(
                            m.Type, m.Arguments[0],
                            (LambdaExpression)StripQuotes(m.Arguments[1]),
                            (LambdaExpression)StripQuotes(m.Arguments[2])
                        );
                    }
                    break;
                case "Join":
                    return this.BindJoin(
                        m.Type, m.Arguments[0], m.Arguments[1],
                        (LambdaExpression)StripQuotes(m.Arguments[2]),
                        (LambdaExpression)StripQuotes(m.Arguments[3]),
                        (LambdaExpression)StripQuotes(m.Arguments[4])
                    );
                case "OrderBy":
                    return this.BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending);
                case "OrderByDescending":
                    return this.BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending);
                case "ThenBy":
                    return this.BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending);
                case "ThenByDescending":
                    return this.BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending);
            }
        }
        return base.VisitMethodCall(m);
    }

    private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
    {
        var projection = (ProjectionExpression)this.Visit(source)!;
        this.m_map[predicate.Parameters[0]] = projection.Projector;
        Expression where = this.Visit(predicate.Body)!;
        string alias = this.GetNextAlias();
        ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, projection.Source.Alias);
        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, projection.Source, where),
            pc.Projector
        );
    }


    private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
    {
        var projection = (ProjectionExpression)this.Visit(source)!;
        this.m_map[selector.Parameters[0]] = projection.Projector;
        Expression expression = this.Visit(selector.Body)!;
        string alias = this.GetNextAlias();
        ProjectedColumns pc = this.ProjectColumns(expression, alias, projection.Source.Alias);
        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, projection.Source, null),
            pc.Projector
        );
    }

    protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression? resultSelector)
    {
        var projection = (ProjectionExpression)this.Visit(source)!;
        this.m_map[collectionSelector.Parameters[0]] = projection.Projector;
        var collectionProjection = (ProjectionExpression)this.Visit(collectionSelector.Body)!;
        JoinType joinType = IsTable(collectionSelector.Body) ? JoinType.CrossJoin : JoinType.CrossApply;
        var join = new JoinExpression(resultType, joinType, projection.Source, collectionProjection.Source, null);
        string alias = this.GetNextAlias();
        ProjectedColumns pc;
        if (resultSelector == null)
        {
            pc = this.ProjectColumns(collectionProjection.Projector, alias, projection.Source.Alias, collectionProjection.Source.Alias);
        }
        else
        {
            this.m_map[resultSelector.Parameters[0]] = projection.Projector;
            this.m_map[resultSelector.Parameters[1]] = collectionProjection.Projector;
            Expression result = this.Visit(resultSelector.Body)!;
            pc = this.ProjectColumns(result, alias, projection.Source.Alias, collectionProjection.Source.Alias);
        }
        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, join, null),
            pc.Projector
        );
    }

    protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
    {
        var outerProjection = (ProjectionExpression)this.Visit(outerSource)!;
        var innerProjection = (ProjectionExpression)this.Visit(innerSource)!;
        this.m_map[outerKey.Parameters[0]] = outerProjection.Projector;
        Expression outerKeyExpr = this.Visit(outerKey.Body)!;
        this.m_map[innerKey.Parameters[0]] = innerProjection.Projector;
        Expression innerKeyExpr = this.Visit(innerKey.Body)!;
        this.m_map[resultSelector.Parameters[0]] = outerProjection.Projector;
        this.m_map[resultSelector.Parameters[1]] = innerProjection.Projector;
        Expression resultExpr = this.Visit(resultSelector.Body)!;
        var join = new JoinExpression(resultType, JoinType.InnerJoin, outerProjection.Source, innerProjection.Source, Expression.Equal(outerKeyExpr, innerKeyExpr));
        string alias = this.GetNextAlias();
        ProjectedColumns pc = this.ProjectColumns(resultExpr, alias, outerProjection.Source.Alias, innerProjection.Source.Alias);
        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, join, null),
            pc.Projector
        );
    }

    List<OrderExpression>? m_thenBys;

    protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
    {
        List<OrderExpression>? myThenBys = this.m_thenBys;
        this.m_thenBys = null;
        var projection = (ProjectionExpression)this.Visit(source)!;

        this.m_map[orderSelector.Parameters[0]] = projection.Projector;
        var orderings = new List<OrderExpression> { new OrderExpression(orderType, this.Visit(orderSelector.Body)!) };

        if (myThenBys != null)
        {
            for (int i = myThenBys.Count - 1; i >= 0; i--)
            {
                OrderExpression tb = myThenBys[i];
                var lambda = (LambdaExpression)tb.Expression;
                this.m_map[lambda.Parameters[0]] = projection.Projector;
                orderings.Add(new OrderExpression(tb.OrderType, this.Visit(lambda.Body)!));
            }
        }

        string alias = this.GetNextAlias();
        ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, projection.Source.Alias);
        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, projection.Source, null, orderings.AsReadOnly()),
            pc.Projector
        );
    }

    protected virtual Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
    {
        if (this.m_thenBys == null)
        {
            this.m_thenBys = new List<OrderExpression>();
        }
        this.m_thenBys.Add(new OrderExpression(orderType, orderSelector));
        return this.Visit(source)!;
    }

    private bool IsTable(Expression expression)
    {
        var c = expression as ConstantExpression;
        return c != null && IsTable(c.Value);
    }

    private bool IsTable(object? value)
    {
        var q = value as IQueryable;
        return q != null && q.Provider == this.m_provider && q.Expression.NodeType == ExpressionType.Constant;
    }

    private string GetTableName(object table)
    {
        var tableQuery = (IQueryable)table;
        Type rowType = tableQuery.ElementType;

        // get the name for attribute

        return rowType.Name;
    }

    private string GetColumnName(MemberInfo member)
    {
        return member.Name;
    }

    private Type GetColumnType(MemberInfo member)
    {
        var fi = member as FieldInfo;
        if (fi != null)
        {
            return fi.FieldType;
        }
        var pi = (PropertyInfo)member;
        return pi.PropertyType;
    }

    private IEnumerable<PropertyInfo> GetMappedMembers(Type rowType)
    {
        return rowType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.PropertyType.Namespace != "MotoRent.Domain");
    }

    private ProjectionExpression GetTableProjection(object value)
    {
        var table = (IQueryable)value;
        string tableAlias = this.GetNextAlias();
        string selectAlias = this.GetNextAlias();
        var bindings = new List<MemberBinding>();
        var columns = new List<ColumnDeclaration>();
        foreach (PropertyInfo mi in this.GetMappedMembers(table.ElementType))
        {
            string columnName = this.GetColumnName(mi);
            Type columnType = this.GetColumnType(mi);
            int ordinal = columns.Count;
            bindings.Add(Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName, ordinal)));

            columns.Add(new ColumnDeclaration(columnName, new ColumnExpression(columnType, tableAlias, columnName, ordinal)));
        }
        Expression projector = Expression.MemberInit(Expression.New(table.ElementType), bindings);
        Type resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);
        return new ProjectionExpression(
            new SelectExpression(
                resultType,
                selectAlias,
                columns,
                new TableExpression(resultType, tableAlias, this.GetTableName(table)),
                null
            ),
            projector
        );
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (this.IsTable(c.Value))
        {
            return GetTableProjection(c.Value!);
        }
        return c;
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
        if (this.m_map.TryGetValue(p, out Expression? e))
        {
            return e;
        }
        return p;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        Expression source = this.Visit(m.Expression)!;
        switch (source.NodeType)
        {
            case ExpressionType.MemberInit:
                var min = (MemberInitExpression)source;
                for (int i = 0, n = min.Bindings.Count; i < n; i++)
                {
                    var assign = min.Bindings[i] as MemberAssignment;
                    if (assign != null && MembersMatch(assign.Member, m.Member))
                    {
                        return assign.Expression;
                    }
                }
                break;
            case ExpressionType.New:
                var nex = (NewExpression)source;
                if (nex.Members != null)
                {
                    for (int i = 0, n = nex.Members.Count; i < n; i++)
                    {
                        if (MembersMatch(nex.Members[i], m.Member))
                        {
                            return nex.Arguments[i];
                        }
                    }
                }
                break;
        }
        if (source == m.Expression)
        {
            return m;
        }
        return MakeMemberAccess(source, m.Member);
    }

    private bool MembersMatch(MemberInfo a, MemberInfo b)
    {
        if (a == b)
        {
            return true;
        }
        if (a is MethodInfo && b is PropertyInfo)
        {
            return a == ((PropertyInfo)b).GetGetMethod();
        }
        if (a is PropertyInfo && b is MethodInfo)
        {
            return ((PropertyInfo)a).GetGetMethod() == b;
        }
        return false;
    }

    private Expression MakeMemberAccess(Expression source, MemberInfo mi)
    {
        var fi = mi as FieldInfo;
        if (fi != null)
        {
            return Expression.Field(source, fi);
        }
        var pi = (PropertyInfo)mi;
        return Expression.Property(source, pi);
    }
}
