using MediatR;
using Microsoft.Extensions.Options;
using ProjectBase.Core.Extensions;
using ProjectBase.Core.Logging;
using ProjectBase.Core.Logging.Models;
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ProjectBase.Core.PipelineBehaviors;

internal partial class LoggingBehaviour<TRequest, TResponse>(IOptions<LoggingOptions> options) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    #region Variables (static readonly)

    private static readonly JsonSerializerOptions JsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false };

    private static readonly Regex[] SensitiveFieldPatterns = [EmailFieldRegex(), PasswordFieldRegex(), TokenFieldRegex()];

    #endregion

    #region Variables (readonly)

    private readonly LoggingOptions OptionsValue = options.Value;

    #endregion

    #region Handle

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        var requestName = typeof(TRequest).Name;
        var correlationId = Activity.Current?.GetOrCreateCorrelationId();

        Activity.Current?.AddTag("request.name", requestName);
        Activity.Current?.AddBaggage("correlation.id", correlationId);

        var requestJson = (string?)null;

        if (OptionsValue.EnableRequestLogging)
        {
            requestJson = SerializeJsonSafe(request);
            requestJson = TruncateIfNeeded(requestJson);
        }

        try
        {
            var response = await next(cancellationToken);

            timer.Stop();

            var responseJson = (string?)null;

            if (OptionsValue.EnableResponseLogging)
            {
                responseJson = SerializeJsonSafe(response);
                responseJson = TruncateIfNeeded(responseJson);
            }

            Log.ForContext(LogFields.CorrelationId, correlationId)
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.ElapsedMs, timer.ElapsedMilliseconds)
               .ForContext(LogFields.RequestBody, requestJson, destructureObjects: false)
               .ForContext(LogFields.ResponseBody, responseJson, destructureObjects: false)
               .ForContext(LogFields.MessageSource, "MediatR")
               // Use existing structured properties to avoid duplicate fields; include correlation id
               .Information("Completed {request.name} in {elapsed.ms} ms (CorrelationId: {correlation.id})");

            return response;
        }
        catch (Exception ex)
        {
            timer.Stop();

            Log.ForContext(LogFields.CorrelationId, correlationId)
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.ElapsedMs, timer.ElapsedMilliseconds)
               .ForContext(LogFields.RequestBody, requestJson, destructureObjects: false)
               .ForContext(LogFields.MessageSource, "MediatR")
               // Use existing structured properties to avoid duplicate fields; include correlation id
               .Error(ex, "Error in {request.name} after {elapsed.ms} ms (CorrelationId: {correlation.id})");

            throw;
        }
    }

    #endregion

    #region Field Regexes

    [GeneratedRegex("(?i)\"email\"\\s*:\\s*\".*?\"", RegexOptions.Compiled, "tr-TR")]
    private static partial Regex EmailFieldRegex();

    [GeneratedRegex("(?i)\"password\"\\s*:\\s*\".*?\"", RegexOptions.Compiled, "tr-TR")]
    private static partial Regex PasswordFieldRegex();

    [GeneratedRegex("(?i)\"token\"\\s*:\\s*\".*?\"", RegexOptions.Compiled, "tr-TR")]
    private static partial Regex TokenFieldRegex();

    #endregion

    #region Helper Methods

    private string SerializeJsonSafe(object? obj)
    {
        try
        {
            var json = JsonSerializer.Serialize(obj, JsonOptions);

            if (OptionsValue.MaskSensitiveData)
                json = MaskSensitiveFields(json);

            return json;
        }
        catch
        {
            return obj?.ToString() ?? "(null)";
        }
    }

    private static string MaskSensitiveFields(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        foreach (var regex in SensitiveFieldPatterns)
        {
            json = regex.Replace(json, m =>
            {
                var colonIndex = m.Value.IndexOf(':');
                if (colonIndex < 0)
                    return m.Value;

                return string.Concat(m.Value.AsSpan(0, colonIndex + 1), "\"***\"");
            });
        }

        return json;
    }

    private string TruncateIfNeeded(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length <= OptionsValue.MaxBodyLength)
            return json;

        return string.Concat(json.AsSpan(0, OptionsValue.MaxBodyLength), "...(truncated)");
    }

    #endregion
}