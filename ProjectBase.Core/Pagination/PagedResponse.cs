using ProjectBase.Core.Results;

namespace ProjectBase.Core.Pagination;

/// <summary>
/// Represents a paged response that extends the Result pattern with pagination metadata.
/// </summary>
/// <typeparam name="T">The type of data returned in the response.</typeparam>
public class PagedResponse<T> : Result<T>
{
    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the total number of records across all pages.
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResponse{T}"/> class.
    /// </summary>
    /// <param name="data">The data for the current page.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="message">Optional message describing the result.</param>
    public PagedResponse(T? data, int pageNumber, int pageSize, string? message = null)
        : this(data, pageNumber, pageSize, ResultType.Success, true, message, null)
    {
    }

    private PagedResponse(
        T? data,
        int pageNumber,
        int pageSize,
        ResultType responseType,
        bool isSuccess,
        string? message,
        List<ErrorResult>? errors)
    {
        ValidateResponseType(responseType, isSuccess);

        PageNumber = pageNumber;
        PageSize = pageSize;

        Data = isSuccess ? data : default;
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors;

        ResponseType = responseType;
        StatusCode = (int)responseType;
    }

    /// <summary>
    /// Creates an empty paged response with default values (page 1, page size 10).
    /// </summary>
    /// <returns>An empty <see cref="PagedResponse{T}"/> instance.</returns>
    public static PagedResponse<T> EmptyPagedResponse() => new(default, 1, 10);

    /// <summary>
    /// Creates a failed empty paged response with default values (page 1, page size 10).
    /// </summary>
    /// <param name="responseType">The result type indicating failure.</param>
    /// <param name="message">Optional message describing the failure.</param>
    /// <returns>A failed empty <see cref="PagedResponse{T}"/> instance.</returns>
    public static PagedResponse<T> FailEmptyPagedResponse(ResultType responseType, string? message = null)
        => new(default, 1, 10, responseType, false, message, null);
}

