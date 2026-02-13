using System.Linq.Expressions;

namespace Vector.Domain.Common;

/// <summary>
/// Base class for specification pattern implementation.
/// Specifications encapsulate query criteria for domain objects.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
public abstract class Specification<T>
{
    /// <summary>
    /// Converts the specification to a LINQ expression.
    /// </summary>
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Checks if the specified entity satisfies the specification.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity satisfies the specification; otherwise, false.</returns>
    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    /// <summary>
    /// Combines this specification with another using logical AND.
    /// </summary>
    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Combines this specification with another using logical OR.
    /// </summary>
    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Creates a negation of this specification.
    /// </summary>
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = left.ToExpression();
        var rightExpression = right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));

        var body = Expression.AndAlso(
            Expression.Invoke(leftExpression, parameter),
            Expression.Invoke(rightExpression, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

internal sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = left.ToExpression();
        var rightExpression = right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));

        var body = Expression.OrElse(
            Expression.Invoke(leftExpression, parameter),
            Expression.Invoke(rightExpression, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

internal sealed class NotSpecification<T>(Specification<T> specification) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var expression = specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));

        var body = Expression.Not(Expression.Invoke(expression, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
