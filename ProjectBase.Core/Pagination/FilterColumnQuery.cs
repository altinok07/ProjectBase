namespace ProjectBase.Core.Pagination;

/// <summary>
/// Represents a filter query for a specific column.
/// </summary>
public class FilterColumnQuery
{
    /// <summary>
    /// Gets or sets the name of the column to filter.
    /// </summary>
    public string? ColumnField { get; set; }

    /// <summary>
    /// Gets or sets the operator to use for filtering (e.g., "equals", "contains", "greaterThan").
    /// </summary>
    public string? OperatorValue { get; set; }

    /// <summary>
    /// Gets or sets the value to filter by.
    /// </summary>
    public string? Value { get; set; }
}

