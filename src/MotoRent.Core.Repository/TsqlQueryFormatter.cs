using System.Collections;
using System.Linq.Expressions;
using System.Text;
using MotoRent.Core.Repository.QueryProviders;

namespace MotoRent.Core.Repository;

internal class TsqlQueryFormatter : DbExpressionVisitor
{
    StringBuilder m_sb = new();
    int m_depth;

    internal string Format(Expression expression)
    {
        this.m_sb = new StringBuilder();
        this.Visit(expression);
        return this.m_sb.ToString();
    }

    protected enum Indentation
    {
        Same,
        Inner,
        Outer
    }

    internal int IndentationWidth { get; set; } = 2;

    private void AppendNewLine(Indentation style)
    {
        m_sb.AppendLine();
        this.Indent(style);
        for (int i = 0, n = this.m_depth * this.IndentationWidth; i < n; i++)
        {
            m_sb.Append(" ");
        }
    }

    private void Indent(Indentation style)
    {
        if (style == Indentation.Inner)
        {
            this.m_depth++;
        }
        else if (style == Indentation.Outer)
        {
            this.m_depth--;
            System.Diagnostics.Debug.Assert(this.m_depth >= 0);
        }
    }

    private string GetPropertyName(Expression expression)
    {
        if (expression is ColumnExpression cl02)
            return cl02.Name;

        var ob = $"{expression}";
        var cutoff = ob.LastIndexOf("}", StringComparison.Ordinal) + 2;
        return ob.Substring(cutoff, ob.Length - cutoff).Replace(".", "");
    }

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        var member = m switch
        {
            { Arguments: [UnaryExpression { Operand: ColumnExpression ce }] } => ce.Name,
            { Arguments: [.., var a] } => GetPropertyName(a),
            _ => ""
        };
        var methodName = m.Method.Name;
        var notIn = "";
        const string IS_IN_LIST = "IsInList";
        const string CONTAINS = "Contains";
        if (m_sb is [.., 'N', 'O', 'T', ' '] && methodName is IS_IN_LIST or CONTAINS)
        {
            notIn = "NOT ";
            m_sb.Remove(m_sb.Length - 4, 4);
        }

        switch (methodName)
        {
            case IS_IN_LIST when m.Object is ConstantExpression { Value: IEnumerable<int> int32List }:
                m_sb.Append($" [{member}] {notIn}IN (");
                m_sb.Append(string.Join(",", int32List));
                m_sb.Append(')');
                return m;
            case IS_IN_LIST when m is
                                 {
                                     Arguments:
                                     [
                                         ConstantExpression
                                         {
                                             Value: IEnumerable<int> t6, Type.FullName: not null
                                         } arg0,
                                         _
                                     ]
                                 }
                                 && !arg0.Type.FullName.StartsWith("MotoRent.Domain."):
                m_sb.Append($" [{member}] {notIn}IN (");
                m_sb.Append(string.Join(",", t6.Select(x => x)));
                m_sb.Append(')');
                return m;
            case IS_IN_LIST when m is
            {
                Arguments:
                [
                    ConstantExpression { Value: IEnumerable<int> t6, Type.FullName: not null },
                    ColumnExpression { Type.IsEnum: true } c1
                ]
            }:
                m_sb.Append($" [{member}] {notIn}IN (");
                m_sb.Append(string.Join(",", t6.Select(x => $"'{Enum.GetName(c1.Type, x)}'")));
                m_sb.Append(')');
                return m;
            case IS_IN_LIST when m is
            {
                Arguments:
                [
                    ConstantExpression { Value: IEnumerable t6, Type.FullName: not null },
                    ColumnExpression { Type : { GenericTypeArguments: [{ IsEnum: true }] } } c1
                ]
            }:
                m_sb.Append($" [{member}] {notIn}IN (");
                var items = new List<string>();
                foreach (var item66 in t6)
                {
                    items.Add($"'{item66}'");
                }
                m_sb.Append(string.Join(",", items));
                m_sb.Append(')');
                return m;
            case IS_IN_LIST when m.Object is ConstantExpression { Value: IEnumerable<int?> int32List }:
                m_sb.Append($" [{member}] {notIn}IN (");
                m_sb.Append(string.Join(",", int32List.Select(x => x switch { null => "NULL", _ => $"{x}" })));
                m_sb.Append(')');
                return m;
            case IS_IN_LIST when m.Arguments is [ConstantExpression { Value: IEnumerable<string> stringList }, _]:
                m_sb.Append($" [{member}] {notIn}IN (");
                m_sb.Append(string.Join(",", stringList.Select(x => $"'{x}'")));
                m_sb.Append(')');
                return m;
            case IS_IN_LIST when m.Object is ConstantExpression { Value: { } ve } ce
                                 && ve.GetType().IsGenericType
                                 && ve.GetType().Name is "ImmutableArray`1"
                                 && ve.GetType().GenericTypeArguments is [{ IsEnum: true }]:
                m_sb.Append($" [{member}] {notIn}IN (");
                dynamic enumList = ve;
                var arg = "'" + string.Join("', '", enumList) + "'";
                m_sb.Append(arg);
                m_sb.Append(')');
                return m;
            case CONTAINS when m is { Object: ConstantExpression { Value: IEnumerable list5 }, Arguments: [var ie] }:
            {
                m_sb.Append($" [{member}] IN (");
                foreach (var item in list5)
                {
                    switch (ie)
                    {
                        case { Type.Name: "String" }:
                        case { Type: { IsGenericType: true, GenericTypeArguments: [{ IsEnum: true }] } }:
                        case { Type.IsEnum: true }:
                            m_sb.Append($"'{item}', ");
                            break;
                        case not null when item is null:
                            m_sb.Append("NULL, ");
                            break;
                        default:
                            m_sb.Append($"{item}, ");
                            break;
                    }
                }
                m_sb.Remove(m_sb.Length - 2, 2);
                m_sb.Append(')');
                return m;
            }
            case CONTAINS when m.Object == null && m.Arguments.Count == 2:
            {
                m_sb.Append($" [{member}] IN (");

                var argType = m.Arguments[0].Type;
                if (argType == typeof(string[]))
                {
                    dynamic list = m.Arguments[0];
                    var flatted = ((string[])list.Value).Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Replace("'", "''"));
                    var cccs = string.Join("','", flatted);
                    m_sb.Append($"'{cccs}'");
                }

                if (argType == typeof(int[]))
                {
                    dynamic list = m.Arguments[0];
                    var cccs = string.Join(",", (int[])list.Value);
                    m_sb.Append(cccs);
                }

                if (argType == typeof(int?[]))
                {
                    dynamic list = m.Arguments[0];
                    var cccs = string.Join(",", (int?[])list.Value);
                    m_sb.Append(cccs);
                }

                if (argType.IsArray)
                {
                    var elementType = argType.GetElementType();
                    if (elementType is { IsEnum: true } || (elementType is { IsGenericType: true } &&
                                                            elementType.GenericTypeArguments[0].IsEnum))
                    {
                        dynamic list = m.Arguments[0];
                        var cccs = "'" + string.Join("', '", list.Value) + "'";
                        m_sb.Append(cccs);
                    }
                }

                m_sb.Append(")");
                return m;
            }
        }

        if (m.Method.Name == "Contains" && m.Object == null && m.Arguments.Count == 2)
        {
            var propertyName01 = GetPropertyName(m.Arguments[1]);
            m_sb.AppendFormat(" [{0}] IN(", propertyName01);

            var argType = m.Arguments[0].Type;
            if (argType == typeof(string[]))
            {
                dynamic list = m.Arguments[0];
                var flatted = ((string[])list.Value).Select(s => s.Replace("'", "''"));
                var cccs = string.Join("','", flatted);
                m_sb.AppendFormat("'{0}'", cccs);
            }

            if (argType == typeof(int[]))
            {
                dynamic list = m.Arguments[0];
                var cccs = string.Join(",", (int[])list.Value);
                m_sb.Append(cccs);
            }

            if (argType == typeof(int?[]))
            {
                dynamic list = m.Arguments[0];
                var cccs = string.Join(",", (int?[])list.Value);
                m_sb.Append(cccs);
            }

            if (argType.IsArray)
            {
                var elementType = argType.GetElementType();
                if (elementType is { IsEnum: true } || (elementType is { IsGenericType: true } &&
                                                        elementType.GenericTypeArguments[0].IsEnum))
                {
                    dynamic list = m.Arguments[0];
                    var cccs = "'" + string.Join("', '", list.Value) + "'";
                    m_sb.Append(cccs);
                }
            }

            m_sb.Append(")");
            return m;
        }

        var method = m.Method;
        var propertyName = GetPropertyName(m.Object!);

        switch (method.Name)
        {
            case "Count":
            case "Distinct":
                m_sb.Append("COUNT(*)");
                return m;
            case "StartsWith":
                m_sb.Append("(");
                m_sb.AppendFormat(" [{0}] LIKE ", propertyName);
                this.Visit(m.Arguments[0]);
                m_sb.Append(" + '%')");
                return m;
            case "Contains":
                m_sb.Append("(");
                m_sb.AppendFormat(" [{0}] LIKE '%' + ", propertyName);
                this.Visit(m.Arguments[0]);
                m_sb.Append(" + '%')");
                return m;
            case "Equals":
                m_sb.Append("(");
                m_sb.AppendFormat(" [{0}] = '", propertyName);
                this.Visit(m.Arguments[0]);
                m_sb.Append("')");
                return m;
            case "EndsWith":
                m_sb.Append("(");
                m_sb.Append($" [{propertyName}] LIKE '%' + ");
                this.Visit(m.Arguments[0]);
                m_sb.Append(")");
                return m;
            default: throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
        }
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Convert:
                this.Visit(u.Operand);
                break;
            case ExpressionType.Not:
                if (u.Operand is ColumnExpression ce && ce.Type == typeof(bool))
                {
                    m_sb.Append($"([{ce.Name}] = 0)");
                    break;
                }

                if (u.Operand is MemberExpression me)
                {
                    m_sb.Append($"([{me.Member.Name}] = 0)");
                    break;
                }

                m_sb.Append(" NOT ");
                this.Visit(u.Operand);
                break;
            default:
                throw new NotSupportedException($"The unary operator '{u.NodeType}' is not supported");
        }

        return u;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        m_sb.Append("(");
        this.Visit(b.Left);
        switch (b.Left)
        {
            case MemberExpression mb:
            {
                if (mb.Expression is MemberExpression inner)
                    m_sb.Append(inner.Member.Name);

                m_sb.Append(mb.Member.Name);
                break;
            }
            case ColumnExpression ce when ce.Type == typeof(bool):
            {
                if (b.Right is not ConstantExpression)
                    m_sb.Append(" = 1 ");
                break;
            }
        }

        switch (b.NodeType)
        {
            case ExpressionType.And:
                m_sb.Append(" AND ");
                break;
            case ExpressionType.Or:
                m_sb.Append(" OR ");
                break;
            case ExpressionType.Equal:
                m_sb.Append(" = ");
                break;
            case ExpressionType.NotEqual:
                m_sb.Append(" <> ");
                break;
            case ExpressionType.LessThan:
                m_sb.Append(" < ");
                break;
            case ExpressionType.LessThanOrEqual:
                m_sb.Append(" <= ");
                break;
            case ExpressionType.GreaterThan:
                m_sb.Append(" > ");
                break;
            case ExpressionType.GreaterThanOrEqual:
                m_sb.Append(" >= ");
                break;
            case ExpressionType.AndAlso:
                m_sb.Append(" AND ");
                break;
            case ExpressionType.OrElse:
                m_sb.Append(" OR ");
                break;
            default:
                throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");
        }

        this.Visit(b.Right);

        m_sb.Append(")");
        return b;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (c.Value == null)
        {
            m_sb.Append("IS NULL");
            m_sb.Replace("= IS NULL", "IS NULL");
        }
        else
        {
            switch (Type.GetTypeCode(c.Value.GetType()))
            {
                case TypeCode.Boolean:
                    m_sb.Append(((bool)c.Value) ? 1 : 0);
                    break;
                case TypeCode.String:
                    m_sb.Append("'");
                    var val = c.Value as string;
                    if (!string.IsNullOrWhiteSpace(val))
                        val = val.Replace("'", "''");
                    m_sb.Append(val);
                    m_sb.Append("'");
                    break;
                case TypeCode.DateTime:
                    m_sb.Append($"'{c.Value:s}'");
                    break;
                case TypeCode.Object:
                    if (c.Value is DateTimeOffset { Year: > 2000 } dto)
                    {
                        m_sb.Append($"'{dto:O}'");
                        break;
                    }

                    if (c.Value is DateOnly { Year: > 2000 } dy)
                    {
                        m_sb.Append($"'{dy:yyyy-MM-dd}'");
                        break;
                    }

                    throw new NotSupportedException($"The constant for '{c.Value}' is not supported");
                default:
                    m_sb.Append(c.Value);
                    break;
            }
        }

        return c;
    }

    protected override Expression VisitColumn(ColumnExpression column)
    {
        m_sb.AppendFormat("[{0}]", column.Name);
        return column;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        m_sb.Append("SELECT ");
        if (select.Columns.Count == 1)
        {
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                var column = select.Columns[i];
                if (column.Name.StartsWith("PropertyName")) continue;
                if (i > 0)
                {
                    m_sb.Append(", ");
                }

                var c = this.Visit(column.Expression) as ColumnExpression;
                if (c == null || c.Name != select.Columns[i].Name)
                {
                    m_sb.Append(column.Name);
                }
            }
        }
        else
        {
            m_sb.Append(" [Data] ");
        }

        if (select.From != null)
        {
            this.AppendNewLine(Indentation.Same);
            m_sb.Append($"FROM [Core].[");
            this.VisitSource(select.From);
            m_sb.Append("] ");
        }

        if (select.Where != null)
        {
            this.AppendNewLine(Indentation.Same);
            m_sb.Append("WHERE ");
            if (select.Where is MemberExpression { CanReduce: false } me)
            {
                m_sb.Append($"([{me.Member.Name}] = 1)");
            }
            else
                this.Visit(select.Where);
        }

        if (select.OrderBy != null && select.OrderBy.Count > 0)
        {
            this.AppendNewLine(Indentation.Same);
            m_sb.Append("ORDER BY ");
            for (int i = 0, n = select.OrderBy.Count; i < n; i++)
            {
                var exp = select.OrderBy[i];
                if (i > 0)
                {
                    m_sb.Append(", ");
                }

                this.Visit(exp.Expression);

                if (exp.OrderType != OrderType.Ascending)
                {
                    m_sb.Append(" DESC");
                }
            }
        }

        return select;
    }

    protected override Expression VisitSource(Expression source)
    {
        switch ((DbExpressionType)source.NodeType)
        {
            case DbExpressionType.Table:
                var table = (TableExpression)source;
                m_sb.Append(table.Name);
                break;
            case DbExpressionType.Select:
                var select = (SelectExpression)source;
                m_sb.Append("(");
                this.AppendNewLine(Indentation.Inner);
                this.Visit(select);
                this.AppendNewLine(Indentation.Outer);
                m_sb.Append(")");
                m_sb.Append(" AS ");
                m_sb.Append(select.Alias);
                break;
            case DbExpressionType.Join:
                this.VisitJoin((JoinExpression)source);
                break;
            default:
                throw new InvalidOperationException("Select source is not valid type");
        }

        return source;
    }

    protected override Expression VisitJoin(JoinExpression join)
    {
        this.VisitSource(join.Left);
        this.AppendNewLine(Indentation.Same);
        switch (join.Join)
        {
            case JoinType.CrossJoin:
                m_sb.Append("CROSS JOIN ");
                break;
            case JoinType.InnerJoin:
                m_sb.Append("INNER JOIN ");
                break;
            case JoinType.CrossApply:
                m_sb.Append("CROSS APPLY ");
                break;
        }

        this.VisitSource(join.Right);
        if (join.Condition != null)
        {
            this.AppendNewLine(Indentation.Inner);
            m_sb.Append("ON ");
            this.Visit(join.Condition);
            this.Indent(Indentation.Outer);
        }

        return join;
    }
}
