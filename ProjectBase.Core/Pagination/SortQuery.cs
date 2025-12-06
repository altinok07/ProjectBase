namespace ProjectBase.Core.Pagination;

/// <summary>
/// Represents a sort query for ordering results.
/// </summary>
public class SortQuery
{
    /// <summary>
    /// Gets or sets the name of the field to sort by.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the sort direction (e.g., "asc", "desc").
    /// </summary>
    public string? Sort { get; set; }
}

