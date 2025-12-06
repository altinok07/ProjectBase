using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace ProjectBase.Core.Pagination;

/// <summary>
/// Represents a request for paged and filtered data with optional includes and ordering.
/// </summary>
/// <typeparam name="T">The type of entity being queried.</typeparam>
public class PagedFilterRequest<T>(PaginationFilterQuery filterQuery,
    Expression<Func<T, bool>>? predicate = null,
    Func<IQueryable<T>, IIncludableQueryable<T, object>>? includePaths = null,
    Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
{
    /// <summary>
    /// Gets or sets the pagination filter query containing page number, page size, and other filters.
    /// </summary>
    public PaginationFilterQuery FilterQuery { get; set; } = filterQuery;

    /// <summary>
    /// Gets or sets the optional predicate expression for filtering the query.
    /// </summary>
    public Expression<Func<T, bool>>? Predicate { get; set; } = predicate;

    /// <summary>
    /// Gets or sets the optional function to include related entities in the query.
    /// </summary>
    public Func<IQueryable<T>, IIncludableQueryable<T, object>>? IncludePaths { get; set; } = includePaths;

    /// <summary>
    /// Gets or sets the optional function to order the query results.
    /// </summary>
    public Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; set; } = orderBy;
}

