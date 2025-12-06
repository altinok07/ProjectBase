using Microsoft.EntityFrameworkCore.Query;
using ProjectBase.Core.Entities;
using System.Linq.Expressions;

namespace ProjectBase.Core.Expressions;

public class GenericExpression<T>(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IIncludableQueryable<T, object>>? includePaths, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy) where T : BaseEntity
{
    public Expression<Func<T, bool>>? Predicate { get; set; } = predicate;
    public Func<IQueryable<T>, IIncludableQueryable<T, object>>? IncludePaths { get; set; } = includePaths;
    public Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; set; } = orderBy;

    public GenericExpression(Expression<Func<T, bool>>? predicate) : this(predicate, null, null) { }

    public GenericExpression(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IIncludableQueryable<T, object>>? includePaths) : this(predicate, includePaths, null) { }

    public static GenericExpression<T> Create(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? includePaths = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
        => new(predicate, includePaths, orderBy);
}

