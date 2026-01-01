using MediatR;
using ProjectBase.Core.Logging;
using Serilog;
using System.Diagnostics;

namespace ProjectBase.Core.PipelineBehaviors;

internal class PerformanceBehaviour<TRequest, TResponse>(int longRunningThresholdMs = 500) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next(cancellationToken);

        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        if (elapsedMilliseconds > longRunningThresholdMs)
        {
            var requestName = typeof(TRequest).Name;

            Log.ForContext(LogFields.CorrelationId, Activity.Current?.GetOrCreateCorrelationId())
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.ElapsedMs, elapsedMilliseconds)
               .ForContext(LogFields.MessageSource, "MediatR")
               .Warning("Long Running Request: {RequestName} took {ElapsedMs} ms. Threshold: {Threshold} ms.", requestName, elapsedMilliseconds, longRunningThresholdMs);
        }

        return response;
    }
}