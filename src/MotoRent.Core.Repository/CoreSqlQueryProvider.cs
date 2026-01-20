using System.IO;
using System.Linq.Expressions;
using MotoRent.Core.Repository.QueryProviders;
using MotoRent.Domain.Core;

namespace MotoRent.Core.Repository;

/// <summary>
/// A LINQ query provider that executes SQL queries over a DbConnection
/// </summary>
public class CoreSqlQueryProvider : CoreQueryProvider
{
    private readonly IRequestContext m_context;

    public CoreSqlQueryProvider(IRequestContext context)
    {
        m_context = context;
    }

    private TextWriter? Log { get; set; }

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

        if (this.Log != null)
        {
            this.Log.WriteLine(query.CommandText);
            this.Log.WriteLine();
        }

        var text = query.CommandText;
        System.Diagnostics.Debug.WriteLine(text);
        var list = new List<object>();

        return list;
    }

    internal class TranslateResult
    {
        internal string CommandText = string.Empty;
        internal LambdaExpression Projector = null!;
    }

    internal TranslateResult Translate(Expression expression)
    {
        var projection = expression as ProjectionExpression;
        if (projection == null)
        {
            expression = Evaluator.PartialEval(expression, CanBeEvaluatedLocally);
            expression = new QueryBinder(this).Bind(expression);
            expression = new OrderByRewriter().Rewrite(expression);
            expression = new UnusedColumnRemover().Remove(expression);
            expression = new RedundantSubqueryRemover().Remove(expression);
            projection = (ProjectionExpression)expression;
        }
        var commandText = new TsqlQueryFormatter().Format(projection.Source);
        var projector = new ProjectionBuilder().Build(projection.Projector, projection.Source.Alias);
        return new TranslateResult { CommandText = commandText, Projector = projector };
    }

    private static bool CanBeEvaluatedLocally(Expression expression)
    {
        return expression.NodeType != ExpressionType.Parameter &&
               expression.NodeType != ExpressionType.Lambda;
    }
}
