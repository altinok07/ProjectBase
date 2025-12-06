namespace ProjectBase.Core.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail, with associated data of type T.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the data associated with a successful result.
    /// This will be null or default if the operation failed.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Private constructor - only factory methods can use this.
    /// </summary>
    public Result()
    {
    }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="responseType">The result type indicating success.</param>
    /// <param name="data">The data to return.</param>
    /// <param name="message">Optional message describing the success.</param>
    /// <returns>A successful Result instance with data.</returns>
    public static Result<T> Success(ResultType responseType, T data, string? message = null)
    {
        ValidateResponseType(responseType, true);

        return new Result<T>
        {
            Data = data,
            IsSuccess = true,
            Message = message,
            Errors = null,
            ResponseType = responseType,
            StatusCode = (int)responseType
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="responseType">The result type indicating failure.</param>
    /// <param name="message">Optional message describing the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public new static Result<T> Fail(ResultType responseType, string? message = null)
    {
        ValidateResponseType(responseType, false);

        return new Result<T>
        {
            IsSuccess = false,
            Data = default,
            Message = message,
            Errors = null,
            ResponseType = responseType,
            StatusCode = (int)responseType
        };
    }

    /// <summary>
    /// Creates a failed result with a list of errors.
    /// </summary>
    /// <param name="responseType">The result type indicating failure.</param>
    /// <param name="message">Optional message describing the failure.</param>
    /// <param name="errors">The list of errors. Cannot be null or empty.</param>
    /// <returns>A failed Result instance with errors.</returns>
    /// <exception cref="ArgumentException">Thrown when errors is null or empty.</exception>
    public new static Result<T> Fail(ResultType responseType, string? message, List<ErrorResult> errors)
    {
        ValidateResponseType(responseType, false);

        if (errors == null || errors.Count == 0)
            throw new ArgumentException("Errors list cannot be null or empty.", nameof(errors));

        return new Result<T>
        {
            IsSuccess = false,
            Data = default,
            Message = message,
            Errors = errors,
            ResponseType = responseType,
            StatusCode = (int)responseType
        };
    }

    /// <summary>
    /// Attempts to get the data value if the result is successful.
    /// </summary>
    /// <param name="value">When this method returns, contains the data if the result is successful; otherwise, the default value.</param>
    /// <returns>true if the result is successful; otherwise, false.</returns>
    public bool TryGetValue(out T? value)
    {
        if (IsSuccess)
        {
            value = Data;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString()
    {
        return IsSuccess
            ? $"Success: {Message ?? "Operation completed successfully"} (Data: {Data?.ToString() ?? "null"})"
            : $"Failure: {Message ?? "Operation failed"} (Status: {StatusCode})";
    }
}