namespace ProjectBase.Core.Pagination;

/// <summary>
/// Represents a grouped filter query that can contain nested groups (parentheses) and filter items.
/// </summary>
public class FilterGroupQuery
{
    /// <summary>
    /// Gets or sets whether this group should be negated (NOT (...)).
    /// </summary>
    public bool? Negate { get; set; }

    /// <summary>
    /// Gets or sets how <see cref="Items"/> and <see cref="Groups"/> should be combined.
    /// Supported: "and" (default), "or".
    /// </summary>
    public string? Logic { get; set; }

    /// <summary>
    /// Gets or sets filter items inside this group.
    /// </summary>
    public List<FilterColumnQuery>? Items { get; set; }

    /// <summary>
    /// Gets or sets nested groups inside this group.
    /// </summary>
    public List<FilterGroupQuery>? Groups { get; set; }
}


