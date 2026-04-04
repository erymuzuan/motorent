using System.Linq.Expressions;
using MotoRent.Domain.QueryProviders;

namespace MotoRent.PostgreSqlRepository;

/// <summary>
/// A LINQ query provider that translates expression trees into PostgreSQL
/// </summary>
public class PgQueryProvider : QueryProvider
{
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

    private TranslateResult Translate(Expression expression)
    {
        var projection = expression as ProjectionExpression;
        if (projection == null)
        {
            expression = Evaluator.PartialEval(expression, CanBeEvaluatedLocally)!;
            expression = new QueryBinder(this).Bind(expression)!;
            expression = new OrderByRewriter().Rewrite(expression)!;
            expression = new UnusedColumnRemover().Remove(expression)!;
            expression = new RedundantSubqueryRemover().Remove(expression)!;
            projection = (ProjectionExpression)expression;
        }

        var commandText = new PgQueryFormatter().Format(projection!.Source);
        var projector = ProjectionBuilder.Build(projection.Projector, projection.Source.Alias);
        return new TranslateResult { CommandText = commandText, Projector = projector };
    }

    private static bool CanBeEvaluatedLocally(Expression expression)
    {
        return expression.NodeType != ExpressionType.Parameter &&
               expression.NodeType != ExpressionType.Lambda;
    }
}
