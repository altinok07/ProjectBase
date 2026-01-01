using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ProjectBase.Core.Helpers;
using ProjectBase.Core.Logging;
using ProjectBase.Core.Logging.Models;
using Serilog;
using Serilog.Events;
using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace ProjectBase.Core.Middlewares;

internal sealed class HttpLoggingMiddleware(RequestDelegate next, IOptions<HttpLoggingOptions> options)
{
    #region DI

    private readonly HttpLoggingOptions optionsValue = options?.Value ?? new HttpLoggingOptions();

    #endregion

    #region Invoke

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (optionsValue.ExcludePaths?.Any(I => context.Request.Path.StartsWithSegments(I, StringComparison.OrdinalIgnoreCase)) == true)
        {
            await next(context);
            return;
        }

        var correlationId = context.GetOrCreateCorrelationId(optionsValue);

        if (context.Response.HasStarted)
        {
            Log.Warning("Response already started for {Method} {Path} - skipping body capture", context.Request.Method, context.Request.Path);
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        var requestBody = "(not-read)";
        var responseBody = string.Empty;
        var originalResponseBody = context.Response.Body;

        try
        {
            #region Pre-Execution

            if (optionsValue.EnableRequestLogging && ShouldReadContentForLogging(context.Request.ContentType))
            {
                if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > optionsValue.MaxRequestBodyBytes)
                {
                    requestBody = $"(content-length {context.Request.ContentLength.Value} > max {optionsValue.MaxRequestBodyBytes} bytes - skipped)";
                }
                else
                {
                    context.Request.EnableBuffering();

                    requestBody = await ReadStreamAsStringAsync(context.Request.Body, optionsValue.MaxRequestBodyBytes, optionsValue.TruncateRequestBody);

                    try
                    {
                        if (context.Request.Body.CanSeek)
                            context.Request.Body.Position = 0;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to reset request body stream position");
                    }
                }

                if (string.IsNullOrEmpty(requestBody) == false && requestBody != "(not-read)")
                {
                    requestBody = MaskHelper.MaybeMask(requestBody, optionsValue);
                }
            }
            else
            {
                requestBody = "(not-logged)";
            }

            if (string.IsNullOrWhiteSpace(optionsValue.CorrelationHeaderName) == false)
            {
                context.Request.Headers[optionsValue.CorrelationHeaderName] = correlationId;
                context.Response.Headers[optionsValue.CorrelationHeaderName] = correlationId;
            }

            #endregion

            #region Execution

            await using var tempResponseStream = new MemoryStream();

            context.Response.Body = tempResponseStream;

            try
            {
                await next(context);

                stopwatch.Stop();

                if (optionsValue.EnableResponseLogging && ShouldReadContentForLogging(context.Response.ContentType))
                {
                    tempResponseStream.Position = 0;

                    responseBody = await ReadStreamAsStringAsync(tempResponseStream, optionsValue.MaxResponseBodyBytes, truncate: true);

                    if (string.IsNullOrWhiteSpace(responseBody) == false)
                        responseBody = MaskHelper.MaybeMask(responseBody, optionsValue);

                    tempResponseStream.Position = 0;
                }
                else
                {
                    responseBody = "(not-logged)";
                }

                LogCompletion(context, optionsValue, correlationId, stopwatch.ElapsedMilliseconds, requestBody, responseBody);

                await tempResponseStream.CopyToAsync(originalResponseBody);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var errorResponse = string.Empty;

                if (optionsValue.EnableResponseLogging && tempResponseStream.Length > 0)
                {
                    tempResponseStream.Position = 0;
                    errorResponse = await ReadStreamAsStringAsync(tempResponseStream, optionsValue.MaxResponseBodyBytes, truncate: true);

                    if (string.IsNullOrWhiteSpace(errorResponse) == false)
                        errorResponse = MaskHelper.MaybeMask(errorResponse, optionsValue);
                }

                LogError(context, optionsValue, correlationId, stopwatch.ElapsedMilliseconds, requestBody, ex, errorResponse);

                throw;
            }

            #endregion
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    #endregion

    #region Helper Methods (Logs)

    private static void LogCompletion(HttpContext context, HttpLoggingOptions optionsValue, string correlationId, long elapsedMs, string requestBody, string responseBody)
    {
        var level = ParseLogLevel(optionsValue.ResponseLogLevel);

        var log = Log.ForContext(LogFields.CorrelationId, correlationId)
                     .ForContext(LogFields.HttpMethod, context.Request.Method)
                     .ForContext(LogFields.HttpPath, context.Request.Path)
                     .ForContext(LogFields.HttpQuery, QueryToObject(context.Request.Query))
                     .ForContext(LogFields.HttpStatusCode, context.Response.StatusCode)
                     .ForContext(LogFields.ElapsedMs, elapsedMs)
                     .ForContext(LogFields.MessageSource, "Http")
                     .ForContext(LogFields.RequestBody, string.IsNullOrWhiteSpace(requestBody) ? "(empty)" : requestBody)
                     .ForContext(LogFields.ResponseBody, string.IsNullOrWhiteSpace(responseBody) ? "(empty)" : responseBody);

        // Use existing structured properties to avoid duplicate fields; include correlation id
        log.Write(level, "HTTP {http.method} {http.path} ({correlation.id}) completed in {elapsed.ms} ms");
    }

    private static void LogError(HttpContext context, HttpLoggingOptions optionsValue, string correlationId, long elapsedMs, string requestBody, Exception ex, string responseBody)
    {
        var level = ParseLogLevel(optionsValue.ErrorLogLevel);

        var log = Log.ForContext(LogFields.CorrelationId, correlationId)
                     .ForContext(LogFields.HttpMethod, context.Request.Method)
                     .ForContext(LogFields.HttpPath, context.Request.Path)
                     .ForContext(LogFields.HttpQuery, QueryToObject(context.Request.Query))
                     .ForContext(LogFields.ElapsedMs, elapsedMs)
                     .ForContext(LogFields.MessageSource, "Http")
                     .ForContext(LogFields.RequestBody, string.IsNullOrWhiteSpace(requestBody) ? "(empty)" : requestBody)
                     .ForContext(LogFields.ResponseBody, string.IsNullOrWhiteSpace(responseBody) ? "(empty)" : responseBody);

        // Use existing structured properties to avoid duplicate fields; include correlation id
        log.Write(level, ex, "HTTP {http.method} {http.path} ({correlation.id}) failed after {elapsed.ms} ms");
    }

    private static LogEventLevel ParseLogLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return LogEventLevel.Information;

        return Enum.TryParse<LogEventLevel>(level, true, out var logEventLevel) ? logEventLevel : LogEventLevel.Information;
    }

    #endregion

    #region Helper Methods (Stream Reading - ArrayPool)

    private static async Task<string> ReadStreamAsStringAsync(Stream stream, int maxBytes, bool truncate)
    {
        if (stream == null)
            return string.Empty;

        if (stream.CanRead == false)
            return string.Empty;

        try
        {
            if (stream is MemoryStream ms && ms.Length == 0) return string.Empty;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check stream length");
        }

        const int bufferSize = 8192;

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            using var msOut = new MemoryStream();

            int totalRead = 0;
            int read;

            while (totalRead < maxBytes && (read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(bufferSize, maxBytes - totalRead)))) > 0)
            {
                await msOut.WriteAsync(buffer.AsMemory(0, read));

                totalRead += read;
            }

            var isTruncated = stream.CanSeek
                ? stream.Length > totalRead
                : (totalRead == maxBytes && stream.CanRead);

            msOut.Position = 0;

            using var reader = new StreamReader(msOut, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            var text = await reader.ReadToEndAsync();

            if (isTruncated && truncate)
                return text + "... (truncated)";

            return text;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    #endregion

    #region Helper Methods (static)

    private static Dictionary<string, object?> QueryToObject(IQueryCollection query)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in query)
        {
            if (kvp.Value.Count > 1)
                dict[kvp.Key] = kvp.Value.ToArray();
            else
                dict[kvp.Key] = kvp.Value.FirstOrDefault();
        }

        return dict;
    }

    private static bool ShouldReadContentForLogging(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        if (contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
            return false;

        return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("form", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}