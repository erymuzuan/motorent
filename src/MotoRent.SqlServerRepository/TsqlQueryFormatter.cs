using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;

namespace MotoRent.SqlServerRepository;

internal class TsqlQueryFormatter(string schema) : DbExpressionVisitor
{
    private StringBuilder m_sb = new();
    private int m_depth;

    internal string Format(Expression expression)
    {
        this.m_sb = new StringBuilder();
        this.Visit(expression);
        return this.m_sb.ToString();
    }

    private enum Indentation
    {
        Same,
        Inner,
        Outer
    }

    internal int IndentationWidth { get; set; } = 2;

    private void AppendNewLine(Indentation style)
    {
        this.m_sb.AppendLine();
        this.Indent(style);
        for (var i = 0; i < this.m_depth * this.IndentationWidth; i++)
            this.m_sb.Append(' ');
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
            Debug.Assert(this.m_depth >= 0);
        }
    }

    private string GetPropertyName(Expression expression)
    {
        if (expression is ColumnExpression cl02)
            return cl02.Name;
        if (expression is MemberInitExpression init)
            return init.NewExpression.Type.Name;

        if (expression is MethodCallExpression { Method.Name: "GetId" } mc and { Object.Type.Name: not null })
            return mc.Object.Type.Name + "Id";

        if (expression is MemberExpression
            {
                Expression: MemberExpression { Member.Name: var parentName } me2
            } mt and { Member.Name: var memberName })
            return parentName + "." + memberName;

        // property expression is an internal class
        var ob = $"{expression}";
        var cutoff = ob.LastIndexOf("}", StringComparison.Ordinal) + 2;
        var propName = ob.Substring(cutoff, ob.Length - cutoff).Replace(".", "");

        return propName;
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
        if (this.m_sb is [.., 'N', 'O', 'T', ' '] && methodName is IS_IN_LIST or CONTAINS)
        {
            notIn = "NOT ";
            this.m_sb.Remove(this.m_sb.Length - 4, 4);
        }

        switch (methodName)
        {
            // local collection should do SELECT * FROM Table WHERE Column IN (1,2,3)
            case IS_IN_LIST when m.Object is ConstantExpression { Value: IEnumerable<int> int32List }:
                var list1 = int32List.ToList();
                if (list1.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" [{member}] {notIn}IN (");
                    this.m_sb.Append(string.Join(",", list1));
                    this.m_sb.Append(')');
                }
                return m;
            case IS_IN_LIST when m is
            {
                Arguments: [ConstantExpression { Value: IEnumerable<int> t6 }, ColumnExpression { Type.IsEnum: false }]
            }:
                var list2 = t6.ToList();
                if (list2.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" [{member}] {notIn}IN (");
                    this.m_sb.Append(string.Join(",", list2));
                    this.m_sb.Append(')');
                }
                return m;
            // enum
            case IS_IN_LIST when m is
                { Arguments: [ConstantExpression { Value: IEnumerable t6, Type.FullName: not null }, _] }:
                var items2 = new List<string>();
                foreach (var item in t6)
                {
                    switch (item)
                    {
                        case decimal:
                        case double:
                        case int:
                            items2.Add($"{item}");
                            break;
                        case null:
                            items2.Add("NULL");
                            continue;
                        default:
                            items2.Add($"'{item}'");
                            break;
                    }
                }

                if (items2.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" [{member}] {notIn}IN (");
                    this.m_sb.Append(string.Join(",", items2));
                    this.m_sb.Append(')');
                }
                return m;
            //nullable enum
            case IS_IN_LIST when m is
            {
                Arguments:
                [
                    ConstantExpression { Value: IEnumerable t6, Type.FullName: not null },
                    ColumnExpression { Type : { GenericTypeArguments: [{ IsEnum: true }] } }
                ]
            }:
                var items = new List<string>();
                foreach (var item in t6)
                {
                    items.Add($"'{item}'");
                }

                if (items.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" [{member}] {notIn}IN (");
                    this.m_sb.Append(string.Join(",", items));
                    this.m_sb.Append(')');
                }
                return m;
            case IS_IN_LIST when m.Object is ConstantExpression { Value: IEnumerable<int?> int32List }:
                var list3 = int32List.ToList();
                if (list3.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" [{member}] {notIn}IN (");
                    this.m_sb.Append(string.Join(",", list3.Select(x => x switch { null => "NULL", _ => $"{x}" })));
                    this.m_sb.Append(')');
                }
                return m;
            case IS_IN_LIST when m.Arguments is [ConstantExpression { Value: IEnumerable<string> stringList }, _]:
                var list4 = stringList.ToList();
                if (list4.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" [{member}] {notIn}IN (");
                    this.m_sb.Append(string.Join(",", list4.Select(x => $"'{x}'")));
                    this.m_sb.Append(')');
                }
                return m;
            case IS_IN_LIST when m.Object is ConstantExpression { Value: { } ve } && ve.GetType().IsGenericType
                                                                                  && ve.GetType().Name is "ImmutableArray`1"
                                                                                  && ve.GetType().GenericTypeArguments is [{ IsEnum: true }]:
                dynamic enumList = ve;
                var itemsEnum = new List<string>();
                foreach (var item in enumList)
                {
                    itemsEnum.Add(item.ToString());
                }

                if (itemsEnum.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" [{member}] {notIn}IN (");
                    var arg = "'" + string.Join("', '", itemsEnum) + "'";
                    this.m_sb.Append(arg);
                    this.m_sb.Append(')');
                }
                return m;
            case CONTAINS when m is { Object: ConstantExpression { Value: IEnumerable list5 }, Arguments: [var ie] }:
            {
                var itemsContains = new List<string>();
                foreach (var item in list5)
                {
                    switch (ie)
                    {
                        case { Type.Name: "String" }:
                        case { Type: { IsGenericType: true, GenericTypeArguments: [{ IsEnum: true }] } }:
                        case { Type.IsEnum: true }:
                            itemsContains.Add($"'{item}'");
                            break;
                        case not null when item is null:
                            itemsContains.Add("NULL");
                            break;
                        default:
                            itemsContains.Add($"{item}");
                            break;
                    }
                }

                if (itemsContains.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" [{member}] {notIn}IN (");
                    this.m_sb.Append(string.Join(", ", itemsContains));
                    this.m_sb.Append(')');
                }
                return m;
            }
            case CONTAINS when m.Object == null && m.Arguments.Count == 2:
            {
                var argType = m.Arguments[0].Type;
                if (argType == typeof(string[]))
                {
                    dynamic list = m.Arguments[0];
                    var flatted = ((string[])list.Value).Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Replace("'", "''")).ToList();
                    if (flatted.Count == 0)
                    {
                        this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                    }
                    else
                    {
                        this.m_sb.Append($" [{member}] {notIn}IN (");
                        var cccs = string.Join("','", flatted);
                        this.m_sb.Append($"'{cccs}'");
                        this.m_sb.Append(")");
                    }
                }
                else if (argType == typeof(int[]))
                {
                    dynamic list = m.Arguments[0];
                    var values = (int[])list.Value;
                    if (values.Length == 0)
                    {
                        this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                    }
                    else
                    {
                        this.m_sb.Append($" [{member}] {notIn}IN (");
                        var cccs = string.Join(",", values);
                        this.m_sb.Append(cccs);
                        this.m_sb.Append(")");
                    }
                }
                else if (argType == typeof(int?[]))
                {
                    dynamic list = m.Arguments[0];
                    var values = (int?[])list.Value;
                    if (values.Length == 0)
                    {
                        this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                    }
                    else
                    {
                        this.m_sb.Append($" [{member}] {notIn}IN (");
                        var cccs = string.Join(",", values);
                        this.m_sb.Append(cccs);
                        this.m_sb.Append(")");
                    }
                }
                else if (argType.IsArray)
                {
                    var elementType = argType.GetElementType();
                    if (elementType is { IsEnum: true } || (elementType is { IsGenericType: true } &&
                                                            elementType.GenericTypeArguments[0].IsEnum))
                    {
                        dynamic list = m.Arguments[0];
                        var values = (IEnumerable)list.Value;
                        var itemsArray = new List<string>();
                        foreach (var val in values) itemsArray.Add(val.ToString());

                        if (itemsArray.Count == 0)
                        {
                            this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                        }
                        else
                        {
                            this.m_sb.Append($" [{member}] {notIn}IN (");
                            var cccs = "'" + string.Join("', '", itemsArray) + "'";
                            this.m_sb.Append(cccs);
                            this.m_sb.Append(")");
                        }
                    }
                }
                else
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }

                return m;
            }
        }

        var method = m.Method;
        string propertyName = GetPropertyName(m.Object!);

        switch (method.Name)
        {
            case "Count":
            case "Distinct":
                this.m_sb.Append("Distinct is not supported");
                return m;
            case "StartsWith":
                this.m_sb.Append($"([{propertyName}] LIKE ");
                this.Visit(m.Arguments[0]);
                this.m_sb.Append(" + '%')");
                return m;
            case CONTAINS:
                this.m_sb.Append($"([{propertyName}] LIKE '%' + ");
                this.Visit(m.Arguments[0]);
                this.m_sb.Append(" + '%')");
                return m;
            case "Equals":
                this.m_sb.Append('(');
                this.m_sb.Append($" [{propertyName}] = '");
                this.Visit(m.Arguments[0]);
                this.m_sb.Append("')");
                return m;
            case "EndsWith":
                this.m_sb.Append($"([{propertyName}] LIKE '%' + ");
                this.Visit(m.Arguments[0]);
                this.m_sb.Append(")");
                return m;
            case "GetId":
                this.m_sb.Append($"[{propertyName}Id]");
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
                    this.m_sb.Append($"([{ce.Name}] = 0)");
                    break;
                }

                if (u.Operand is MemberExpression me)
                {
                    this.m_sb.Append($"([{me.Member.Name}] = 0)");
                    break;
                }


                this.m_sb.Append(" NOT ");
                this.Visit(u.Operand);
                break;
            default:
                throw new NotSupportedException($"The unary operator '{u.NodeType}' is not supported");
        }

        return u;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        this.m_sb.Append("(");
        this.Visit(b.Left);

        switch (b.Left)
        {
            case MemberExpression { CanReduce: false, Expression: MemberExpression mb2 } mb1:

                this.m_sb.Append($"[{mb2.Member.Name}.{mb1.Member.Name}]");
                break;
            case MemberExpression { CanReduce: false } mb1:
                if (mb1.Member is PropertyInfo pi && pi.PropertyType == typeof(bool))
                    this.m_sb.Append($"([{mb1.Member.Name}] = 1)");
                else
                    this.m_sb.Append($"[{mb1.Member.Name}]");
                break;
            case UnaryExpression { Operand.Type: { IsGenericType: true, Name: "Nullable`1" } } temp500:

                if (temp500.Operand is MemberExpression { Type.GenericTypeArguments: [{ IsEnum: true }] } mt500)
                {
                    this.m_sb.Append($"[{mt500.Member.Name}]");
                }

                break;
            case MemberExpression mb:
            {
                if (mb.Expression is MemberExpression inner)
                    this.m_sb.Append(inner.Member.Name);

                this.m_sb.Append(mb.Member.Name);
                break;
            }
            case ColumnExpression ce when ce.Type == typeof(bool):
            {
                if (b.Right is not ConstantExpression && (b.Left.CanReduce))
                    this.m_sb.Append(" = 1 ");
                break;
            }
        }

        switch (b.NodeType)
        {
            case ExpressionType.And:
                this.m_sb.Append(" AND ");
                break;
            case ExpressionType.Or:
                this.m_sb.Append(" OR ");
                break;
            case ExpressionType.Equal:
                this.m_sb.Append(" = ");


                break;
            case ExpressionType.NotEqual:
                this.m_sb.Append(" <> ");

                break;
            case ExpressionType.LessThan:
                this.m_sb.Append(" < ");
                break;
            case ExpressionType.LessThanOrEqual:
                this.m_sb.Append(" <= ");
                break;
            case ExpressionType.GreaterThan:
                this.m_sb.Append(" > ");
                break;
            case ExpressionType.GreaterThanOrEqual:
                this.m_sb.Append(" >= ");
                break;
            case ExpressionType.AndAlso:
                this.m_sb.Append(" AND ");
                break;
            case ExpressionType.OrElse:
                this.m_sb.Append(" OR ");
                break;
            default:
                throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");
        }


        // case for enum
        if (b.Left is UnaryExpression { Operand.Type.IsEnum: true } ue &&
            b.Right is ConstantExpression { Value: int ce2Value })
        {
            var ev = Enum.GetName(ue.Operand.Type, ce2Value);
            this.m_sb.Append($"'{ev}')");
            return b;
        }

        // case for nullable enum
        if (b.Left is UnaryExpression { Operand.Type: { IsGenericType: true, Name: "Nullable`1" } } temp
            && temp.Operand is MemberExpression { Type.GenericTypeArguments: [{ IsEnum: true } nue] } && b.Right is ConstantExpression { Value: int nce2Value }
           )
        {
            var ev = Enum.GetName(nue, nce2Value);
            this.m_sb.Append($"'{ev}')");
            return b;
        }


        // case for enum?
        if (b.Left is UnaryExpression { Operand.Type: { IsGenericType: true, GenericTypeArguments.Length: 1 } } ue2
            && ue2.Operand.Type.GenericTypeArguments[0] is { IsEnum: true } enumType
            && b.Right is ConstantExpression { Value: > 0 } ce3)
        {
            var ev = Enum.GetName(enumType, ce3.Value);
            this.m_sb.Append($"'{ev}')");
            return b;
        }

        this.Visit(b.Right);
        if (m_sb is [.., 'A', 'N', 'D', ' '] && b.Right is MemberExpression { Type.Name: "Boolean" } b5)
        {
            this.m_sb.Append($"([{b5.Member.Name}] = 1)");
        }

        this.m_sb.Append(")");
        return b;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (c.Value == null)
        {
            // Enum != null
            if (this.m_sb.ToString().TrimEnd().EndsWith("<>"))
            {
                this.m_sb.Append(" IS NOT NULL");
                this.m_sb.Replace("<> IS NOT NULL", " IS NOT NULL");
                this.m_sb.Replace(" <>  IS NOT NULL", " IS NOT NULL");
            }
            else
            {
                this.m_sb.Append("IS NULL");
                this.m_sb.Replace("= IS NULL", "IS NULL");
            }
        }
        else
        {
            switch (c.Value)
            {
                case bool bv:
                    this.m_sb.Append(bv ? 1 : 0);
                    break;
                case string val:
                    this.m_sb.Append('\'');
                    if (!string.IsNullOrWhiteSpace(val))
                        val = val.Replace("'", "''");
                    this.m_sb.Append(val);
                    this.m_sb.Append('\'');
                    break;
                // Thai Buddhist calendar handling
                case DateOnly { Year: > 3000 } dt:
                    this.m_sb.Append($"'{dt.AddYears(-1086):yyyy-MM-dd}'");
                    break;
                case DateTime { Year: > 3000 } dt:
                    this.m_sb.Append($"'{dt.AddYears(-1086):s}'");
                    break;
                case DateTimeOffset { Year: > 3000 } dto:
                    this.m_sb.Append($"'{dto.AddYears(-1086):O}'");
                    break;
                case DateOnly { Year: > 2500 } dt:
                    this.m_sb.Append($"'{dt.AddYears(-543):yyyy-MM-dd}'");
                    break;
                case DateTime { Year: > 2500 } dt:
                    this.m_sb.Append($"'{dt.AddYears(-543):s}'");
                    break;
                case DateTimeOffset { Year: > 2500 } dto:
                    this.m_sb.Append($"'{dto.AddYears(-543):O}'");
                    break;
                case DateOnly dt:
                    this.m_sb.Append($"'{dt.Year}-{dt:MM-dd}'");
                    break;
                case DateTime dt:
                    this.m_sb.Append($"'{dt:s}'");
                    break;
                case DateTimeOffset dto:
                    this.m_sb.Append($"'{dto:O}'");
                    break;
                default:
                    this.m_sb.Append(c.Value);
                    break;
            }
        }

        return c;
    }

    protected override Expression? VisitColumn(ColumnExpression column)
    {
        if (column.Type == typeof(bool))
        {
            this.m_sb.Append($"([{column.Name}] = 1)");
            return null;
        }

        this.m_sb.Append($"[{column.Name}]");
        return column;
    }

    protected override Expression VisitMemberAccess(MemberExpression m)
    {
        if (m is { Type.IsEnum: true })
        {
            this.m_sb.Append($"[{m.Member.Name}]");
            return base.VisitMemberAccess(m);
        }

        return base.VisitMemberAccess(m);
    }


    protected override Expression VisitSelect(SelectExpression select)
    {
        this.m_sb.Append("SELECT ");

        // Check if FROM is a subquery - if so, use * to preserve all columns for WHERE clause
        var isSubquerySource = select.From is SelectExpression;

        if (isSubquerySource)
        {
            // Use * for subquery sources to preserve all columns for WHERE filtering
            this.m_sb.Append("* ");
        }
        else if (select.Columns.Count == 1) // just for single
        {
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                var column = select.Columns[i];
                if (column.Name.StartsWith("PropertyName")) continue;
                if (i > 0)
                {
                    this.m_sb.Append(", ");
                }

                var c = this.Visit(column.Expression) as ColumnExpression;
                if (c == null || c.Name != select.Columns[i].Name)
                {
                    this.m_sb.Append(column.Name);
                }
            }
        }
        else
        {
            if (select.From is TableExpression { Type.GenericTypeArguments: [var te1] })
                this.m_sb.Append($" [{te1.Name}Id], [Json] ");
        }


        if (select.From != null)
        {
            this.AppendNewLine(Indentation.Same);
            // Handle subqueries differently - don't wrap with schema brackets
            if (isSubquerySource)
            {
                this.m_sb.Append("FROM ");
                this.VisitSource(select.From);
                this.m_sb.Append(" ");
            }
            else
            {
                this.m_sb.Append($"FROM [{schema}].[");
                this.VisitSource(select.From);
                this.m_sb.Append("] ");
            }
        }

        if (select.Where != null)
        {
            this.AppendNewLine(Indentation.Same);
            this.m_sb.Append("WHERE ");

            if (select.Where is MemberExpression { CanReduce: false } me)
            {
                this.m_sb.Append($"([{me.Member.Name}] = 1)");
            }
            else
                this.Visit(select.Where);
        }

        if (select.OrderBy is { Count: > 0 })
        {
            this.AppendNewLine(Indentation.Same);
            this.m_sb.Append("ORDER BY ");
            for (int i = 0, n = select.OrderBy.Count; i < n; i++)
            {
                var exp = select.OrderBy[i];
                if (i > 0)
                {
                    this.m_sb.Append(", ");
                }

                this.Visit(exp.Expression);

                // nullable DateOnly
                if (exp.Expression.Type is { IsGenericType: true, GenericTypeArguments: [{ Name: "DateOnly" }] })
                {
                    if (exp.Expression is MemberExpression me)
                    {
                        this.m_sb.Append($"[{me.Member.Name}]");
                    }
                }

                if (exp.Expression.Type is { Name: nameof(Decimal) } && exp.Expression is MemberExpression dm)
                    this.m_sb.Append($"[{dm.Member.Name}]");

                if (exp.OrderType != OrderType.Ascending)
                {
                    this.m_sb.Append(" DESC");
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
                if (table.Type.GenericTypeArguments is [var et])
                    this.m_sb.Append(et.Name);
                else
                    this.m_sb.Append(table.Name);
                break;
            case DbExpressionType.Select:
                var select = (SelectExpression)source;
                this.m_sb.Append('(');
                this.AppendNewLine(Indentation.Inner);
                this.Visit(select);
                this.AppendNewLine(Indentation.Outer);
                this.m_sb.Append(')');
                this.m_sb.Append(" AS ");
                this.m_sb.Append(select.Alias);
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
                this.m_sb.Append("CROSS JOIN ");
                break;
            case JoinType.InnerJoin:
                this.m_sb.Append("INNER JOIN ");
                break;
            case JoinType.CrossApply:
                this.m_sb.Append("CROSS APPLY ");
                break;
        }

        this.VisitSource(join.Right);
        if (join.Condition != null)
        {
            this.AppendNewLine(Indentation.Inner);
            this.m_sb.Append("ON ");
            this.Visit(join.Condition);
            this.Indent(Indentation.Outer);
        }

        return join;
    }
}
