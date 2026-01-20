using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace MotoRent.Core.Repository.QueryProviders;

/// <summary>
/// Extended node types for custom expressions
/// </summary>
public enum DbExpressionType
{
    Table = 1000, // make sure these don't overlap with ExpressionType
    Column,
    Select,
    Projection,
    Join
}

public static class DbExpressionExtensions
{
    public static bool IsDbExpression(this ExpressionType et)
    {
        return ((int) et) >= 1000;
    }
}

/// <summary>
/// A custom expression node that represents a table reference in a SQL query
/// </summary>
public class TableExpression : Expression
{
    public TableExpression(Type type, string alias, string name)
    {
        Type = type;
        Alias = alias;
        Name = name;
    }

    public override Type Type { get; }
    public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Table;
    public string Alias { get; }

    public string Name { get; }
}

/// <summary>
/// A custom expression node that represents a reference to a column in a SQL query
/// </summary>
public class ColumnExpression : Expression
{
    public ColumnExpression(Type type, string alias, string name, int ordinal)
    {
        Type = type;
        Alias = alias;
        this.Name = name;
        this.Ordinal = ordinal;
    }
    public override Type Type { get; }

    public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Column;
    public string Alias { get; }

    public string Name { get; }

    public int Ordinal { get; }
}

/// <summary>
/// A declaration of a column in a SQL SELECT expression
/// </summary>
public class ColumnDeclaration
{
    public ColumnDeclaration(string name, Expression expression)
    {
        this.Name = name;
        this.Expression = expression;
    }

    public string Name { get; }

    public Expression Expression { get; }
}

/// <summary>
/// An SQL OrderBy order type
/// </summary>
public enum OrderType
{
    Ascending,
    Descending
}

/// <summary>
/// A pairing of an expression and an order type for use in a SQL Order By clause
/// </summary>
public class OrderExpression
{
    public OrderExpression(OrderType orderType, Expression expression)
    {
        this.OrderType = orderType;
        this.Expression = expression;
    }

    public OrderType OrderType { get; }

    public Expression Expression { get; }
}

/// <summary>
/// A custom expression node used to represent a SQL SELECT expression
/// </summary>
public class SelectExpression : Expression
{

    public SelectExpression(
        Type type, string alias, IEnumerable<ColumnDeclaration> columns,
        Expression from, Expression? where, IEnumerable<OrderExpression>? orderBy)
    {
        Type = type;
        this.Alias = alias;
        this.Columns = columns as ReadOnlyCollection<ColumnDeclaration> ??
                       new List<ColumnDeclaration>(columns).AsReadOnly();
        this.From = from;
        this.Where = where;
        this.OrderBy = orderBy as ReadOnlyCollection<OrderExpression>;
        if (this.OrderBy == null && orderBy != null)
        {
            this.OrderBy = new List<OrderExpression>(orderBy).AsReadOnly();
        }
    }
    public override Type Type { get; }

    public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Select;

    public SelectExpression(
        Type type, string alias, IEnumerable<ColumnDeclaration> columns,
        Expression @from, Expression? @where)
        : this(type, alias, columns, @from, @where, null)
    {
    }

    public string Alias { get; }

    public ReadOnlyCollection<ColumnDeclaration> Columns { get; }

    public Expression From { get; }

    public Expression? Where { get; }


    public ReadOnlyCollection<OrderExpression>? OrderBy { get; }
}

/// <summary>
/// A kind of SQL join
/// </summary>
public enum JoinType
{
    CrossJoin,
    InnerJoin,
    CrossApply,
}

/// <summary>
/// A custom expression node representing a SQL join clause
/// </summary>
public class JoinExpression : Expression
{
    public JoinExpression(Type type, JoinType joinType, Expression left, Expression right, Expression? condition)
    {
        Type = type;
        this.Join = joinType;
        this.Left = left;
        this.Right = right;
        this.Condition = condition;
    }
    public override Type Type { get; }

    public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Join;

    public JoinType Join { get; }

    public Expression Left { get; }

    public Expression Right { get; }

    public new Expression? Condition { get; }
}

/// <summary>
/// A custom expression representing the construction of one or more result objects from a
/// SQL select expression
/// </summary>
public class ProjectionExpression : Expression
{
    public ProjectionExpression(SelectExpression source, Expression projector)
    {
        this.Source = source;
        this.Projector = projector;
    }
    public override Type Type => Source.Type;

    public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Projection;
    public SelectExpression Source { get; }

    public Expression Projector { get; }
}

/// <summary>
/// An extended expression visitor including custom DbExpression nodes
/// </summary>
public class DbExpressionVisitor : ExpressionVisitor
{
    public override Expression? Visit(Expression? exp)
    {
        if (exp == null)
        {
            return null;
        }

        return (DbExpressionType) exp.NodeType switch
        {
            DbExpressionType.Table => VisitTable((TableExpression) exp),
            DbExpressionType.Column => VisitColumn((ColumnExpression) exp),
            DbExpressionType.Select => VisitSelect((SelectExpression) exp),
            DbExpressionType.Join => VisitJoin((JoinExpression) exp),
            DbExpressionType.Projection => VisitProjection((ProjectionExpression) exp),
            _ => base.Visit(exp)
        };
    }

    protected virtual Expression VisitTable(TableExpression table)
    {
        return table;
    }

    protected virtual Expression VisitColumn(ColumnExpression column)
    {
        return column;
    }

    protected virtual Expression VisitSelect(SelectExpression select)
    {
        Expression from = VisitSource(select.From);
        Expression? where = Visit(select.Where);
        ReadOnlyCollection<ColumnDeclaration> columns = VisitColumnDeclarations(select.Columns);
        ReadOnlyCollection<OrderExpression>? orderBy = VisitOrderBy(select.OrderBy);
        if (from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy)
        {
            return new SelectExpression(select.Type, select.Alias, columns, from, where, orderBy);
        }
        return select;
    }

    protected virtual Expression VisitJoin(JoinExpression join)
    {
        Expression left = VisitSource(join.Left);
        Expression right = VisitSource(join.Right);
        Expression? condition = Visit(join.Condition);
        if (left != join.Left || right != join.Right || condition != join.Condition)
        {
            return new JoinExpression(join.Type, join.Join, left, right, condition);
        }
        return join;
    }

    protected virtual Expression VisitSource(Expression source)
    {
        return Visit(source)!;
    }

    protected virtual Expression VisitProjection(ProjectionExpression proj)
    {
        var source = (SelectExpression) Visit(proj.Source)!;
        Expression projector = Visit(proj.Projector)!;
        if (source != proj.Source || projector != proj.Projector)
        {
            return new ProjectionExpression(source, projector);
        }
        return proj;
    }

    protected ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(
        ReadOnlyCollection<ColumnDeclaration> columns)
    {
        List<ColumnDeclaration>? alternate = null;
        for (int i = 0, n = columns.Count; i < n; i++)
        {
            ColumnDeclaration column = columns[i];
            Expression e = Visit(column.Expression)!;
            if (alternate == null && e != column.Expression)
            {
                alternate = columns.Take(i).ToList();
            }
            if (alternate != null)
            {
                alternate.Add(new ColumnDeclaration(column.Name, e));
            }
        }
        if (alternate != null)
        {
            return alternate.AsReadOnly();
        }
        return columns;
    }

    protected ReadOnlyCollection<OrderExpression>? VisitOrderBy(ReadOnlyCollection<OrderExpression>? expressions)
    {
        if (expressions != null)
        {
            List<OrderExpression>? alternate = null;
            for (int i = 0, n = expressions.Count; i < n; i++)
            {
                OrderExpression expr = expressions[i];
                Expression e = Visit(expr.Expression)!;
                if (alternate == null && e != expr.Expression)
                {
                    alternate = expressions.Take(i).ToList();
                }
                if (alternate != null)
                {
                    alternate.Add(new OrderExpression(expr.OrderType, e));
                }
            }
            if (alternate != null)
            {
                return alternate.AsReadOnly();
            }
        }
        return expressions;
    }
}
