namespace ProjectBase.Core.Pagination;

/// <summary>
/// Represents a simple paged data model with total records count.
/// </summary>
/// <typeparam name="T">The type of items in the paged data.</typeparam>
public class PagedModel<T>(IEnumerable<T> pagedData, int totalRecords)
{
    /// <summary>
    /// Gets or sets the total number of records across all pages.
    /// </summary>
    public int TotalRecords { get; set; } = totalRecords;

    /// <summary>
    /// Gets or sets the paged data for the current page.
    /// </summary>
    public IEnumerable<T> PagedData { get; set; } = pagedData;
}

