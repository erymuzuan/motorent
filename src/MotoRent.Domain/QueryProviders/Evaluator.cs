using System.Linq.Expressions;

namespace MotoRent.Domain.QueryProviders;

public static class Evaluator {
    /// <summary>
    /// Performs evaluation & replacement of independent sub-trees
    /// </summary>
    /// <param name="expression">The root of the expression tree.</param>
    /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
    public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated) {
        return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
    }

    /// <summary>
    /// Performs evaluation & replacement of independent sub-trees
    /// </summary>
    /// <param name="expression">The root of the expression tree.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
    public static Expression PartialEval(Expression expression) {
        return PartialEval(expression, CanBeEvaluatedLocally);
    }

    private static bool CanBeEvaluatedLocally(Expression expression) {
        return expression.NodeType != ExpressionType.Parameter;
    }

    /// <summary>
    /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
    /// </summary>
    class SubtreeEvaluator: DbExpressionVisitor {
        readonly HashSet<Expression> m_candidates;

        internal SubtreeEvaluator(HashSet<Expression> candidates) {
            this.m_candidates = candidates;
        }

        internal Expression Eval(Expression exp) {
            return this.Visit(exp)!;
        }

        protected override Expression? Visit(Expression? exp) {
            if (exp == null) {
                return null;
            }
            if (this.m_candidates.Contains(exp)) {
                return this.Evaluate(exp);
            }
            return base.Visit(exp);
        }

        private Expression Evaluate(Expression e) {
            if (e.NodeType == ExpressionType.Constant) {
                return e;
            }
            var lambda = Expression.Lambda(e);
            var fn = lambda.Compile();
            try
            {
                var t = fn.DynamicInvoke();
                return Expression.Constant(t, e.Type);
            }
            catch (NotSupportedException)
            {
                throw new InvalidOperationException($"You might have .Contains in your expression'{e}', change it to IsInList");
            }
        }
    }

    /// <summary>
    /// Performs bottom-up analysis to determine which nodes can possibly
    /// be part of an evaluated sub-tree.
    /// </summary>
    class Nominator : DbExpressionVisitor {
        readonly Func<Expression, bool> m_fnCanBeEvaluated;
        HashSet<Expression> m_candidates = new();
        bool m_cannotBeEvaluated;

        internal Nominator(Func<Expression, bool> fnCanBeEvaluated) {
            this.m_fnCanBeEvaluated = fnCanBeEvaluated;
        }

        internal HashSet<Expression> Nominate(Expression expression) {
            this.m_candidates = new HashSet<Expression>();
            this.Visit(expression);
            return this.m_candidates;
        }

        protected override Expression? Visit(Expression? expression) {
            if (expression != null) {
                bool saveCannotBeEvaluated = this.m_cannotBeEvaluated;
                this.m_cannotBeEvaluated = false;
                base.Visit(expression);
                if (!this.m_cannotBeEvaluated) {
                    if (this.m_fnCanBeEvaluated(expression)) {
                        this.m_candidates.Add(expression);
                    }
                    else {
                        this.m_cannotBeEvaluated = true;
                    }
                }
                this.m_cannotBeEvaluated |= saveCannotBeEvaluated;
            }
            return expression;
        }
    }
}
