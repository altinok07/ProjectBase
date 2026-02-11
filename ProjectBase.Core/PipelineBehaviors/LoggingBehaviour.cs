using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ProjectBase.Core.Helpers;
using ProjectBase.Core.Logging;
using ProjectBase.Core.Logging.Models;
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectBase.Core.PipelineBehaviors;

internal class LoggingBehaviour<TRequest, TResponse>(IOptions<HttpLoggingOptions> options, IHttpContextAccessor httpContextAccessor) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    #region Variables (static readonly)

    private static readonly JsonSerializerOptions JsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false };

    #endregion

    #region Variables (readonly)

    private readonly HttpLoggingOptions OptionsValue = options.Value;

    #endregion

    #region Handle

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        var requestName = typeof(TRequest).Name;
        var correlationId = Activity.Current?.GetOrCreateCorrelationId();
        var userId = httpContextAccessor.HttpContext.GetUserIdOrAnonymous();

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

            var httpStatusCode = httpContextAccessor.HttpContext?.Response.StatusCode;

            Log.ForContext(LogFields.CorrelationId, correlationId)
               .ForContext(LogFields.UserId, userId)
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.ElapsedMs, timer.ElapsedMilliseconds)
               .ForContext(LogFields.RequestBody, requestJson, destructureObjects: false)
               .ForContext(LogFields.ResponseBody, responseJson, destructureObjects: false)
               .ForContext(LogFields.HttpStatusCode, httpStatusCode)
               .ForContext(LogFields.MessageSource, "MediatR")
               // Use existing structured properties to avoid duplicate fields; include correlation id
               .Information("Completed {request.name} in {elapsed.ms} ms (CorrelationId: {correlation.id}) - Status: {http.status.code}");

            return response;
        }
        catch (Exception ex)
        {
            timer.Stop();

            var httpStatusCode = httpContextAccessor.HttpContext?.Response.StatusCode;

            Log.ForContext(LogFields.CorrelationId, correlationId)
               .ForContext(LogFields.UserId, userId)
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.ElapsedMs, timer.ElapsedMilliseconds)
               .ForContext(LogFields.RequestBody, requestJson, destructureObjects: false)
               .ForContext(LogFields.HttpStatusCode, httpStatusCode)
               .ForContext(LogFields.MessageSource, "MediatR")
               // Use existing structured properties to avoid duplicate fields; include correlation id
               .Error(ex, "Error in {request.name} after {elapsed.ms} ms (CorrelationId: {correlation.id}) - Status: {http.status.code}");

            throw;
        }
    }

    #endregion

    #region Helper Methods

    private string SerializeJsonSafe(object? obj)
    {
        try
        {
            var json = JsonSerializer.Serialize(obj, JsonOptions);
            return MaskHelper.MaybeMask(json, OptionsValue);
        }
        catch
        {
            return obj?.ToString() ?? "(null)";
        }
    }

    private string TruncateIfNeeded(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length <= OptionsValue.MaxBodyLength)
            return json;

        return string.Concat(json.AsSpan(0, OptionsValue.MaxBodyLength), "...(truncated)");
    }

    #endregion
}
