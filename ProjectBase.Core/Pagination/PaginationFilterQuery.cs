namespace ProjectBase.Core.Pagination;

/// <summary>
/// Represents a query filter for pagination with optional keyword search, column filters, and sorting.
/// </summary>
public class PaginationFilterQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationFilterQuery"/> class with default values.
    /// </summary>
    public PaginationFilterQuery()
    {
        PageNumber = 1;
        PageSize = 10;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationFilterQuery"/> class with specified page number and page size.
    /// </summary>
    /// <param name="pageNumber">The page number (will be set to 1 if less than 1).</param>
    /// <param name="pageSize">The page size (will be capped at 10 if greater than 10).</param>
    public PaginationFilterQuery(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize > 10 ? 10 : pageSize;
    }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets grouped filters (parentheses support).
    /// If set, this takes precedence over <see cref="FilterItems"/>.
    /// </summary>
    public FilterGroupQuery? FilterGroup { get; set; }

    /// <summary>
    /// Gets or sets multiple column filters for advanced filtering.
    /// If set, this takes precedence over <see cref="Filters"/>.
    /// </summary>
    public List<FilterColumnQuery>? FilterItems { get; set; }

    /// <summary>
    /// Gets or sets how <see cref="FilterItems"/> should be combined.
    /// Supported: "and" (default), "or".
    /// </summary>
    public string? FilterLogic { get; set; }

    /// <summary>
    /// Gets or sets multiple sort queries for ordering results.
    /// First item becomes OrderBy, the rest ThenBy.
    /// </summary>
    public List<SortQuery>? Sorts { get; set; }
}

/// <summary>
/// Represents a pagination filter query with a strongly-typed filter object.
/// </summary>
/// <typeparam name="TFilterQuery">The type of the filter query object.</typeparam>
public class PaginationFilterQuery<TFilterQuery> : PaginationFilterQuery
{
    /// <summary>
    /// Gets or sets the strongly-typed filter query object.
    /// </summary>
    public TFilterQuery? Filter { get; set; }
}

