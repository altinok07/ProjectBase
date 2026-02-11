using Serilog;
using System.Diagnostics;

namespace ProjectBase.Core.Logging;

/// <summary>
/// Hangfire job'ları için logging helper. MediatR ve RabbitMQ logging'i ile tutarlı yapıda.
/// </summary>
public static class HangfireLoggingHelper
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    /// <summary>
    /// Job başlangıcında log yazar ve correlation ID'yi saklar.
    /// </summary>
    public static string LogJobStart(string jobName, string? correlationId = null)
    {
        correlationId ??= Activity.Current.GetOrCreateCorrelationId();
        CurrentCorrelationId.Value = correlationId;

        Log.ForContext(LogFields.CorrelationId, correlationId)
           .ForContext(LogFields.RequestName, jobName)
           .ForContext(LogFields.MessageSource, "Hangfire")
           .Information("Starting Hangfire job: {request.name} (CorrelationId: {correlation.id})");

        return correlationId;
    }

    /// <summary>
    /// Job başarıyla tamamlandığında log yazar. Aynı correlation ID'yi kullanır.
    /// </summary>
    public static void LogJobCompleted(string jobName, long elapsedMs, string? correlationId = null)
    {
        correlationId ??= CurrentCorrelationId.Value ?? Activity.Current.GetOrCreateCorrelationId();

        Log.ForContext(LogFields.CorrelationId, correlationId)
           .ForContext(LogFields.RequestName, jobName)
           .ForContext(LogFields.ElapsedMs, elapsedMs)
           .ForContext(LogFields.MessageSource, "Hangfire")
           // Use existing structured properties to avoid duplicate fields; include correlation id
           .Information("Completed {request.name} in {elapsed.ms} ms (CorrelationId: {correlation.id})");
    }

    /// <summary>
    /// Job hata aldığında log yazar. Aynı correlation ID'yi kullanır.
    /// </summary>
    public static void LogJobFailed(string jobName, Exception exception, long elapsedMs, string? correlationId = null)
    {
        correlationId ??= CurrentCorrelationId.Value ?? Activity.Current.GetOrCreateCorrelationId();

        Log.ForContext(LogFields.CorrelationId, correlationId)
           .ForContext(LogFields.RequestName, jobName)
           .ForContext(LogFields.ElapsedMs, elapsedMs)
           .ForContext(LogFields.MessageSource, "Hangfire")
           // Use existing structured properties to avoid duplicate fields; include correlation id
           .Error(exception, "Error in {request.name} after {elapsed.ms} ms (CorrelationId: {correlation.id})");
    }
}

