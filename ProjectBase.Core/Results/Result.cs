using System.Text.Json.Serialization;

namespace ProjectBase.Core.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// This is a non-generic version that does not carry data.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the optional message associated with the result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public List<ErrorResult>? Errors { get; init; }

    /// <summary>
    /// Gets the HTTP status code associated with the result.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets the result type enum value.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ResultType ResponseType { get; init; }

    /// <summary>
    /// Protected constructor - only factory methods and derived classes can use this.
    /// </summary>
    protected Result()
    {
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="responseType">The result type indicating success.</param>
    /// <param name="message">Optional message describing the success.</param>
    /// <returns>A successful Result instance.</returns>
    public static Result Success(ResultType responseType, string? message = null)
    {
        ValidateResponseType(responseType, true);

        return new()
        {
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
    public static Result Fail(ResultType responseType, string? message = null)
    {
        ValidateResponseType(responseType, false);

        return new()
        {
            IsSuccess = false,
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
    public static Result Fail(ResultType responseType, string? message, List<ErrorResult> errors)
    {
        ValidateResponseType(responseType, false);

        if (errors == null || errors.Count == 0)
            throw new ArgumentException("Errors list cannot be null or empty.", nameof(errors));

        return new()
        {
            IsSuccess = false,
            Message = message,
            ResponseType = responseType,
            StatusCode = (int)responseType,
            Errors = errors
        };
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString()
    {
        return IsSuccess
            ? $"Success: {Message ?? "Operation completed successfully"}"
            : $"Failure: {Message ?? "Operation failed"} (Status: {StatusCode})";
    }

    /// <summary>
    /// Validates that the response type matches the expected success/failure state.
    /// </summary>
    /// <param name="responseType">The response type to validate.</param>
    /// <param name="expectedSuccess">True if expecting a success type, false if expecting a failure type.</param>
    /// <exception cref="ArgumentException">Thrown when the response type doesn't match the expected state.</exception>
    protected static void ValidateResponseType(ResultType responseType, bool expectedSuccess)
    {
        bool isSuccessType = responseType == ResultType.Success ||
                            responseType == ResultType.Created ||
                            responseType == ResultType.NoContent;

        if (expectedSuccess && !isSuccessType)
        {
            throw new ArgumentException(
                $"ResponseType '{responseType}' is not a success type. Use Success, Created, or NoContent.",
                nameof(responseType));
        }

        if (!expectedSuccess && isSuccessType)
        {
            throw new ArgumentException(
                $"ResponseType '{responseType}' is a success type. Use a failure type for Fail method.",
                nameof(responseType));
        }
    }
}

