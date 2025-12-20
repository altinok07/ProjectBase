using FluentValidation;
using Microsoft.AspNetCore.Http;
using ProjectBase.Core.Results;
using Serilog;
using System.Net;
using System.Text.Json;

namespace ProjectBase.Core.Middlewares;

/// <summary>
/// Middleware to handle exceptions globally and return appropriate JSON responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Response zaten başladıysa veya yazıldıysa, hiçbir şey yapma
        if (context.Response.HasStarted)
        {
            _logger.Warning("Response has already started, cannot handle exception");
            return;
        }

        context.Response.ContentType = "application/json";
        Result result;

        switch (exception)
        {
            case ValidationException validationException:
                // FluentValidation ValidationException'ı yakala
                var errors = validationException.Errors
                    .Select(e => new ErrorResult(
                        propertyName: e.PropertyName,
                        errorMessage: e.ErrorMessage,
                        errorCode: e.ErrorCode))
                    .ToList();

                result = Result.Fail(ResultType.BadRequest, "Validation failed", errors);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            default:
                // Diğer exception'lar için generic error
                _logger.Error(exception, "An unhandled exception occurred");

                var genericErrors = new List<ErrorResult>
                {
                    new ErrorResult(
                        propertyName: null,
                        errorMessage: "An error occurred while processing your request.",
                        errorCode: exception.GetType().Name)
                };

                result = Result.Fail(ResultType.InternalServerError, "An internal server error occurred", genericErrors);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var jsonResponse = JsonSerializer.Serialize(result, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

