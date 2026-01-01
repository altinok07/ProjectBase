using MassTransit;
using Microsoft.AspNetCore.Http;
using ProjectBase.Core.Logging.Models;
using System.Diagnostics;

namespace ProjectBase.Core.Logging;

internal static class CorrelationIdExtensions
{
    private static string CreateNewId() => Guid.NewGuid().ToString("N");

    private static string GetHeaderName(HttpLoggingOptions? options)
    {
        if (options == null || string.IsNullOrWhiteSpace(options.CorrelationHeaderName))
            return HttpLoggingOptions.DefaultCorrelationHeader;

        return options.CorrelationHeaderName;
    }

    public static string GetOrCreateCorrelationId(this Activity? activity)
    {
        if (activity == null)
            return CreateNewId();

        var traceId = activity.TraceId.ToString();
        return string.IsNullOrWhiteSpace(traceId) ? CreateNewId() : traceId;
    }

    public static string GetOrCreateCorrelationId(this ConsumeContext context, HttpLoggingOptions options)
    {
        var headerName = GetHeaderName(options);

        if (context.Headers.TryGetHeader(headerName, out var header) && header != null)
        {
            var headerValue = header.ToString();
            if (string.IsNullOrWhiteSpace(headerValue) == false)
                return headerValue;
        }

        var contextId = context.CorrelationId?.ToString();

        if (string.IsNullOrWhiteSpace(contextId) == false)
            return contextId;

        return Activity.Current?.GetOrCreateCorrelationId() ?? CreateNewId();
    }

    public static string GetOrCreateCorrelationId(this HttpContext context, HttpLoggingOptions options)
    {
        var headerName = GetHeaderName(options);

        if (context.Request.Headers.TryGetValue(headerName, out var header) && string.IsNullOrWhiteSpace(header) == false)
            return header.ToString();

        return Activity.Current?.GetOrCreateCorrelationId() ?? CreateNewId();
    }
}