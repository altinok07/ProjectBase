using MassTransit;
using Microsoft.Extensions.Options;
using ProjectBase.Core.Extensions;
using ProjectBase.Core.Helpers;
using ProjectBase.Core.Logging.Models;
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectBase.Core.Logging;

public sealed class LoggingConsumerFilter<T>(IOptions<HttpLoggingOptions> options) : IFilter<ConsumeContext<T>> where T : class
{
    #region DI

    private readonly HttpLoggingOptions optionsValue = options?.Value ?? new HttpLoggingOptions();

    #endregion

    #region JsonOptions

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    #endregion

    #region Send

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var correlationId = context.GetOrCreateCorrelationId(optionsValue);

        var stopwatch = Stopwatch.StartNew();

        var requestName = typeof(T).Name;

        var requestJson = SerializeMessage(context.Message, optionsValue);

        try
        {
            await next.Send(context);

            stopwatch.Stop();

            Log.ForContext(LogFields.CorrelationId, correlationId)
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.ElapsedMs, stopwatch.ElapsedMilliseconds)
               .ForContext(LogFields.RequestBody, requestJson, destructureObjects: false)
               .ForContext(LogFields.MessageSource, "RabbitMQ")
               .Information("Completed {RequestName} in {ElapsedMs} ms (CorrelationId: {CorrelationId})", requestName, stopwatch.ElapsedMilliseconds, correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.ForContext(LogFields.CorrelationId, correlationId)
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.ElapsedMs, stopwatch.ElapsedMilliseconds)
               .ForContext(LogFields.RequestBody, requestJson, destructureObjects: false)
               .ForContext(LogFields.MessageSource, "RabbitMQ")
               .Error(ex, "Error in {RequestName} (CorrelationId: {CorrelationId}) after {ElapsedMs} ms", requestName, correlationId, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    #endregion

    #region Masstransit Diagnostic

    public void Probe(ProbeContext context) => context.CreateFilterScope("LoggingConsumerFilter");

    #endregion

    #region Helper Methods (static)

    private static string SerializeMessage(T message, HttpLoggingOptions optionsValue)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, jsonOptions);

            if (optionsValue.MaskSensitiveData)
                json = MaskHelper.MaybeMask(json, optionsValue);

            if (optionsValue.TruncateRequestBody && json.Length > optionsValue.MaxRequestBodyBytes)
                json = json[..optionsValue.MaxRequestBodyBytes] + "...(truncated)";

            return json;
        }
        catch
        {
            return message?.ToString() ?? "(unserializable)";
        }
    }

    #endregion
}