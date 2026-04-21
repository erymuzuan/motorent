using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using MotoRent.Domain.QueryProviders;

namespace MotoRent.PostgreSqlRepository;

internal class PgQueryFormatter : DbExpressionVisitor
{
    private StringBuilder m_sb = new();
    private int m_depth;

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
        this.m_sb.AppendLine();
        this.Indent(style);
        for (int i = 0, n = this.m_depth * this.IndentationWidth; i < n; i++)
        {
            this.m_sb.Append(" ");
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
        if (expression is MemberInitExpression init)
            return init.NewExpression.Type.Name;

        // GetId() support from MotoRent TsqlQueryFormatter
        if (expression is MethodCallExpression { Method.Name: "GetId" } mc and { Object.Type.Name: not null })
            return mc.Object.Type.Name + "Id";

        if (expression is MemberExpression
            {
                Expression: MemberExpression { Member.Name: var parentName }
            } mt and { Member.Name: var memberName })
            return parentName + "." + memberName;

        var ob = $"{expression}";
        var cutoff = ob.LastIndexOf("}", StringComparison.Ordinal) + 2;
        return ob.Substring(cutoff, ob.Length - cutoff).Replace(".", "");
    }

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        var member = m switch
        {
            { Arguments: [UnaryExpression { Operand: ColumnExpression ce }] } => ce.Name,
            { Arguments: [.., var a] } => this.GetPropertyName(a),
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
            case IS_IN_LIST when m.Object is ConstantExpression { Value: IEnumerable<int> int32List }:
                var list1 = int32List.ToList();
                if (list1.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
                    this.m_sb.Append(string.Join(",", list1));
                    this.m_sb.Append(')');
                }
                return m;
            case IS_IN_LIST when m is
            {
                Arguments: [ConstantExpression { Value: IEnumerable<int> t6, Type.FullName: not null } arg0, ColumnExpression { Type.IsEnum: false }]
            } && !arg0.Type.FullName.StartsWith("MotoRent.Domain."):
                var list2 = t6.ToList();
                if (list2.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
                    this.m_sb.Append(string.Join(",", list2));
                    this.m_sb.Append(')');
                }
                return m;
            // enum
            case IS_IN_LIST when m is
            {
                Arguments:
                [
                    ConstantExpression { Value: IEnumerable<int> t6, Type.FullName: not null },
                    ColumnExpression { Type.IsEnum: true } c1
                ]
            }:
                var enumItems = t6.ToList();
                if (enumItems.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
                    this.m_sb.Append(string.Join(",", enumItems.Select(x => $"'{Enum.GetName(c1.Type, x)}'")));
                    this.m_sb.Append(')');
                }
                return m;
            // enum from IEnumerable (general)
            case IS_IN_LIST when m is
            {
                Arguments:
                [
                    ConstantExpression { Value: IEnumerable t6, Type.FullName: not null },
                    _
                ]
            }:
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
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
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
                    ColumnExpression { Type: { GenericTypeArguments: [{ IsEnum: true }] } }
                ]
            }:
                var items = new List<string>();
                foreach (var item66 in t6)
                {
                    items.Add($"'{item66}'");
                }

                if (items.Count == 0)
                {
                    this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                }
                else
                {
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
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
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
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
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
                    this.m_sb.Append(string.Join(",", list4.Select(x => $"'{x}'")));
                    this.m_sb.Append(')');
                }
                return m;
            case IS_IN_LIST when m.Object is ConstantExpression { Value: { } ve }
                                 && ve.GetType().IsGenericType
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
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
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
                    this.m_sb.Append($" \"{member}\" {notIn}IN (");
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
                        this.m_sb.Append($" \"{member}\" {notIn}IN (");
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
                        this.m_sb.Append($" \"{member}\" {notIn}IN (");
                        this.m_sb.Append(string.Join(",", values));
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
                        this.m_sb.Append($" \"{member}\" {notIn}IN (");
                        this.m_sb.Append(string.Join(",", values));
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
                        foreach (var val in values) itemsArray.Add(val.ToString()!);

                        if (itemsArray.Count == 0)
                        {
                            this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
                        }
                        else
                        {
                            this.m_sb.Append($" \"{member}\" {notIn}IN (");
                            var cccs = "'" + string.Join("', '", itemsArray) + "'";
                            this.m_sb.Append(cccs);
                            this.m_sb.Append(")");
                        }
                    }
                    else
                    {
                        this.m_sb.Append(notIn == "" ? " 1=0 " : " 1=1 ");
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
        var propertyName = this.GetPropertyName(m.Object!);

        switch (method.Name)
        {
            case "Count":
            case "Distinct":
                this.m_sb.Append("COUNT placeholder");
                return m;
            case "StartsWith":
                this.m_sb.Append("(");
                this.m_sb.AppendFormat(" \"{0}\" LIKE ", propertyName);
                this.Visit(m.Arguments[0]);
                this.m_sb.Append(" || '%')");
                return m;
            case "Contains":
                this.m_sb.Append("(");
                this.m_sb.AppendFormat(" \"{0}\" LIKE '%' || ", propertyName);
                this.Visit(m.Arguments[0]);
                this.m_sb.Append(" || '%')");
                return m;
            case "Equals":
                this.m_sb.Append("(");
                this.m_sb.AppendFormat(" \"{0}\" = '", propertyName);
                this.Visit(m.Arguments[0]);
                this.m_sb.Append("')");
                return m;
            case "EndsWith":
                this.m_sb.Append("(");
                this.m_sb.Append($" \"{propertyName}\" LIKE '%' || ");
                this.Visit(m.Arguments[0]);
                this.m_sb.Append(")");
                return m;
            case "GetId":
                this.m_sb.Append($"\"{propertyName}Id\"");
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
                    this.m_sb.Append($"(\"{ce.Name}\" = false)");
                    break;
                }

                if (u.Operand is MemberExpression me)
                {
                    this.m_sb.Append($"(\"{me.Member.Name}\" = false)");
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
                this.m_sb.Append($"\"{mb2.Member.Name}.{mb1.Member.Name}\"");
                break;
            case MemberExpression { CanReduce: false } mb1:
                if (mb1.Member is PropertyInfo pi && pi.PropertyType == typeof(bool))
                    this.m_sb.Append($"(\"{mb1.Member.Name}\" = true)");
                else
                    this.m_sb.Append($"\"{mb1.Member.Name}\"");
                break;
            case UnaryExpression { Operand.Type: { IsGenericType: true, Name: "Nullable`1" } } temp500:
                if (temp500.Operand is MemberExpression { Type.GenericTypeArguments: [{ IsEnum: true }] } mt500)
                {
                    this.m_sb.Append($"\"{mt500.Member.Name}\"");
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
                if (b.Right is not ConstantExpression && b.Left.CanReduce)
                    this.m_sb.Append(" = true ");
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

        // Enum comparison: resolve enum names as strings for PostgreSQL
        if (b.NodeType is ExpressionType.Equal or ExpressionType.NotEqual
            && TryResolveEnumComparison(b, out var enumString))
        {
            this.m_sb.Append(enumString);
        }
        else
        {
            this.Visit(b.Right);
        }

        if (m_sb is [.., 'A', 'N', 'D', ' '] && b.Right is MemberExpression { Type.Name: "Boolean" } b5)
        {
            this.m_sb.Append($"(\"{b5.Member.Name}\" = true)");
        }

        this.m_sb.Append(")");
        return b;
    }

    private static bool TryResolveEnumComparison(BinaryExpression b, out string result)
    {
        result = "";

        var enumType = GetEnumTypeFromExpression(b.Left);
        if (enumType == null)
            return false;

        // Direct int constant
        if (b.Right is ConstantExpression { Value: int intVal })
        {
            var name = Enum.GetName(enumType, intVal);
            if (name != null) { result = $"'{name}'"; return true; }
        }

        // Convert wrapping a constant
        if (b.Right is UnaryExpression { NodeType: ExpressionType.Convert, Operand: ConstantExpression { Value: int intVal2 } })
        {
            var name = Enum.GetName(enumType, intVal2);
            if (name != null) { result = $"'{name}'"; return true; }
        }

        return false;
    }

    private static Type? GetEnumTypeFromExpression(Expression expr)
    {
        // Convert(ColumnExpression(enum type), Int32)
        if (expr is UnaryExpression { NodeType: ExpressionType.Convert, Operand: ColumnExpression uce })
        {
            if (uce.Type.IsEnum)
                return uce.Type;
            if (uce.Type is { IsGenericType: true, GenericTypeArguments: [{ IsEnum: true }] })
                return uce.Type.GenericTypeArguments[0];
        }

        // UnaryExpression wrapping MemberExpression for nullable enums
        if (expr is UnaryExpression { NodeType: ExpressionType.Convert, Operand.Type: { IsGenericType: true, Name: "Nullable`1" } } temp
            && temp.Operand is MemberExpression { Type.GenericTypeArguments: [{ IsEnum: true } nue] })
        {
            return nue;
        }

        // Direct enum column
        if (expr is ColumnExpression ce)
        {
            if (ce.Type.IsEnum)
                return ce.Type;
            if (ce.Type is { IsGenericType: true, GenericTypeArguments: [{ IsEnum: true }] })
                return ce.Type.GenericTypeArguments[0];
        }

        return null;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (c.Value == null)
        {
            // Handle <> null -> IS NOT NULL
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
            // Handle enums
            if (c.Value.GetType().IsEnum)
            {
                var enumName = Enum.GetName(c.Value.GetType(), c.Value);
                this.m_sb.Append($"'{enumName}'");
                return c;
            }

            switch (c.Value)
            {
                case bool bv:
                    this.m_sb.Append(bv ? "true" : "false");
                    break;
                case string val:
                    this.m_sb.Append("'");
                    if (!string.IsNullOrWhiteSpace(val))
                        val = val.Replace("'", "''");
                    this.m_sb.Append(val);
                    this.m_sb.Append("'");
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
                case DateTimeOffset { Year: > 2000 } dto:
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
        this.m_sb.AppendFormat("\"{0}\"", column.Name);
        return column;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        this.m_sb.Append("SELECT ");

        if (select.Columns.Count == 1)
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
            this.m_sb.Append(" \"Data\" ");
        }

        if (select.From != null)
        {
            this.AppendNewLine(Indentation.Same);
            this.m_sb.Append("FROM ");
            this.VisitSource(select.From);
            this.m_sb.Append(" ");
        }

        if (select.Where != null)
        {
            this.AppendNewLine(Indentation.Same);
            this.m_sb.Append("WHERE ");
            if (select.Where is MemberExpression { CanReduce: false } me)
            {
                this.m_sb.Append($"(\"{me.Member.Name}\" = true)");
            }
            else
                this.Visit(select.Where);
        }

        if (select.OrderBy != null && select.OrderBy.Count > 0)
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

                // nullable DateOnly ordering support
                if (exp.Expression.Type is { IsGenericType: true, GenericTypeArguments: [{ Name: "DateOnly" }] })
                {
                    if (exp.Expression is MemberExpression me2)
                    {
                        this.m_sb.Append($"\"{me2.Member.Name}\"");
                    }
                }

                // decimal ordering support
                if (exp.Expression.Type is { Name: nameof(Decimal) } && exp.Expression is MemberExpression dm)
                    this.m_sb.Append($"\"{dm.Member.Name}\"");

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
                this.m_sb.Append($"\"{table.Name}\"");
                break;
            case DbExpressionType.Select:
                var select = (SelectExpression)source;
                this.m_sb.Append("(");
                this.AppendNewLine(Indentation.Inner);
                this.Visit(select);
                this.AppendNewLine(Indentation.Outer);
                this.m_sb.Append(")");
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
                this.m_sb.Append("CROSS JOIN LATERAL ");
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
