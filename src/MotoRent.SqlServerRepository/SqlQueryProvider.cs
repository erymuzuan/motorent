using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;
using MotoRent.Domain.QueryProviders;

namespace MotoRent.SqlServerRepository;

/// <summary>
/// A LINQ query provider that executes SQL queries using expression tree translation.
/// </summary>
public class SqlQueryProvider(IRequestContext context) : QueryProvider
{
    private IRequestContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// Creates a new SQL connection using the connection string from the context.
    /// </summary>
    public SqlConnection CreateConnection() => new(this.Context.GetConnectionString());

    public override string GetQueryText(Expression expression)
    {
        return this.Translate(expression).CommandText;
    }

    public override object? Execute(Expression expression)
    {
        return this.Execute(this.Translate(expression));
    }

    private object Execute(TranslateResult query)
    {
        query.Projector.Compile();
        var text = query.CommandText;
        System.Diagnostics.Debug.WriteLine(text);
        var list = new List<object>();
        return list;
    }

    internal class TranslateResult
    {
        internal string CommandText = "";
        internal LambdaExpression Projector = null!;
    }

    internal TranslateResult Translate(Expression expression)
    {
        if (expression is not ProjectionExpression projection)
        {
            expression = Evaluator.PartialEval(expression, CanBeEvaluatedLocally);
            expression = new QueryBinder(this).Bind(expression);
            expression = new OrderByRewriter().Rewrite(expression);
            expression = new UnusedColumnRemover().Remove(expression);
            expression = new RedundantSubqueryRemover().Remove(expression);
            expression = new SubqueryMerger().Merge(expression);
            projection = (ProjectionExpression)expression;
        }

        var account = this.Context.GetSchema();
        var count = 0;
        while (string.IsNullOrWhiteSpace(account) && count < 5)
        {
            account = this.Context.GetSchema();
            count++;
        }

        if (string.IsNullOrWhiteSpace(account))
            throw new InvalidOperationException($"Cannot read the schema/account from {this.Context.GetType().Name}");

        var commandText = new TsqlQueryFormatter(account).Format(projection.Source);
        var projector = ProjectionBuilder.Build(projection.Projector, projection.Source.Alias);
        return new TranslateResult { CommandText = commandText, Projector = projector };
    }

    private static bool CanBeEvaluatedLocally(Expression expression)
    {
        return expression.NodeType != ExpressionType.Parameter &&
               expression.NodeType != ExpressionType.Lambda;
    }
}
