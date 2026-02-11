using System.Linq.Expressions;

namespace ProjectBase.Core.Expressions;

/// <summary>
/// Helper class for building and combining LINQ expressions dynamically.
/// </summary>
public static class PredicateBuilder
{
    /// <summary>
    /// Combines two expressions with AND logic.
    /// </summary>
    public static Expression<Func<T, bool>> CombineAnd<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
    {
        if (a == null) return b;
        if (b == null) return a;

        ParameterExpression p = a.Parameters[0];

        SubstExpressionVisitor visitor = new SubstExpressionVisitor();
        visitor.AddSubstitution(b.Parameters[0], p);

        Expression body = Expression.AndAlso(a.Body, visitor.Visit(b.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }

    /// <summary>
    /// Combines multiple expressions with AND logic.
    /// Example: CombineAnd(A, B, C) => A & B & C
    /// </summary>
    public static Expression<Func<T, bool>> CombineAnd<T>(params Expression<Func<T, bool>>[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            throw new ArgumentException("At least one expression is required.", nameof(expressions));

        Expression<Func<T, bool>>? result = null;
        foreach (var expr in expressions)
        {
            if (expr == null) continue;
            result = result == null ? expr : CombineAnd(result, expr);
        }

        return result ?? throw new ArgumentException("At least one non-null expression is required.", nameof(expressions));
    }

    /// <summary>
    /// Combines two expressions with OR logic.
    /// </summary>
    public static Expression<Func<T, bool>> CombineOr<T>(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
    {
        if (a == null) return b;
        if (b == null) return a;

        ParameterExpression p = a.Parameters[0];

        SubstExpressionVisitor visitor = new SubstExpressionVisitor();
        visitor.AddSubstitution(b.Parameters[0], p);

        Expression body = Expression.OrElse(a.Body, visitor.Visit(b.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }

    /// <summary>
    /// Combines multiple expressions with OR logic.
    /// Example: CombineOr(A, B, C) => A || B || C
    /// </summary>
    public static Expression<Func<T, bool>> CombineOr<T>(params Expression<Func<T, bool>>[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            throw new ArgumentException("At least one expression is required.", nameof(expressions));

        Expression<Func<T, bool>>? result = null;
        foreach (var expr in expressions)
        {
            if (expr == null) continue;
            result = result == null ? expr : CombineOr(result, expr);
        }

        return result ?? throw new ArgumentException("At least one non-null expression is required.", nameof(expressions));
    }

    internal class SubstExpressionVisitor : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> subst = new();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (subst.TryGetValue(node, out var newValue))
            {
                return newValue;
            }
            return node;
        }

        internal void AddSubstitution(ParameterExpression oldParam, ParameterExpression newParam)
        {
            subst[oldParam] = newParam;
        }
    }
}
