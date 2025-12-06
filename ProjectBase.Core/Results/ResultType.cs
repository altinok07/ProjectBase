namespace ProjectBase.Core.Results;

/// <summary>
/// Represents the type of result, mapped to HTTP status codes.
/// </summary>
public enum ResultType
{
    /// <summary>
    /// The operation was successful (HTTP 200).
    /// </summary>
    Success = 200,

    /// <summary>
    /// A resource was successfully created (HTTP 201).
    /// </summary>
    Created = 201,

    /// <summary>
    /// The operation was successful but no content is returned (HTTP 204).
    /// </summary>
    NoContent = 204,

    /// <summary>
    /// The request was invalid or malformed (HTTP 400).
    /// </summary>
    BadRequest = 400,

    /// <summary>
    /// The request requires authentication (HTTP 401).
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// The request is forbidden (HTTP 403).
    /// </summary>
    Forbidden = 403,

    /// <summary>
    /// The requested resource was not found (HTTP 404).
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// The request conflicts with the current state (HTTP 409).
    /// </summary>
    Conflict = 409,

    /// <summary>
    /// The request was well-formed but contains semantic errors (HTTP 422).
    /// </summary>
    UnprocessableEntity = 422,

    /// <summary>
    /// Too many requests have been made (HTTP 429).
    /// </summary>
    TooManyRequests = 429,

    /// <summary>
    /// An internal server error occurred (HTTP 500).
    /// </summary>
    InternalServerError = 500
}

