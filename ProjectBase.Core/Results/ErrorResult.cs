namespace ProjectBase.Core.Results;

/// <summary>
/// Represents a validation or error detail with property name, error message, and optional error code.
/// FluentValidation ile uyumlu yapı.
/// </summary>
public class ErrorResult
{
    /// <summary>
    /// Gets or sets the name of the property that caused the error.
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Gets or sets the error message describing what went wrong.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets an optional error code for programmatic error handling.
    /// </summary>
    public string? ErrorCode { get; init; }


    /// <summary>
    /// Creates a new ErrorResult instance.
    /// </summary>
    public ErrorResult()
    {
    }

    /// <summary>
    /// Creates a new ErrorResult instance with the specified values.
    /// </summary>
    public ErrorResult(string? propertyName, string? errorMessage, string? errorCode = null)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

}

