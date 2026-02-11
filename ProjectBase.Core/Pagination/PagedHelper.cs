namespace ProjectBase.Core.Pagination;

/// <summary>
/// Helper class for creating paged responses.
/// </summary>
public static class PagedHelper
{
    /// <summary>
    /// Creates a paged response with calculated total pages.
    /// </summary>
    /// <typeparam name="T">The type of items in the paged data.</typeparam>
    /// <param name="pagedData">The data for the current page.</param>
    /// <param name="validFilter">The pagination filter query containing page number and page size.</param>
    /// <param name="totalRecords">The total number of records across all pages.</param>
    /// <returns>A <see cref="PagedResponse{T}"/> instance with calculated pagination metadata.</returns>
    public static PagedResponse<IEnumerable<T>> CreatePagedResponse<T>(IEnumerable<T> pagedData, PaginationFilterQuery validFilter, int totalRecords)
    {
        var response = new PagedResponse<IEnumerable<T>>(pagedData, validFilter.PageNumber, validFilter.PageSize);

        var totalPages = totalRecords / (double)validFilter.PageSize;

        var roundedTotalPages = Convert.ToInt32(Math.Ceiling(totalPages));

        response.TotalPages = roundedTotalPages;

        response.TotalRecords = totalRecords;

        return response;
    }

    /// <summary>
    /// Creates a paged response for a non-enumerable payload (e.g. an object that contains multiple collections).
    /// </summary>
    /// <typeparam name="T">The type of the data payload.</typeparam>
    /// <param name="data">The data payload for the current page.</param>
    /// <param name="validFilter">The pagination filter query containing page number and page size.</param>
    /// <param name="totalRecords">The total number of records across all pages (for the primary collection).</param>
    /// <returns>A <see cref="PagedResponse{T}"/> instance with calculated pagination metadata.</returns>
    public static PagedResponse<T> CreatePagedResponseData<T>(T data, PaginationFilterQuery validFilter, int totalRecords)
    {
        var response = new PagedResponse<T>(data, validFilter.PageNumber, validFilter.PageSize);

        var totalPages = totalRecords / (double)validFilter.PageSize;
        response.TotalPages = Convert.ToInt32(Math.Ceiling(totalPages));
        response.TotalRecords = totalRecords;

        return response;
    }
}

