using MediatR;
using Microsoft.AspNetCore.Http;
using ProjectBase.Core.Logging;
using Serilog;
using System.Diagnostics;

namespace ProjectBase.Core.PipelineBehaviors;

internal class PerformanceBehaviour<TRequest, TResponse>(IHttpContextAccessor httpContextAccessor, int longRunningThresholdMs = 500) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
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
            var userId = httpContextAccessor.HttpContext.GetUserIdOrAnonymous();

            Log.ForContext(LogFields.CorrelationId, Activity.Current?.GetOrCreateCorrelationId())
               .ForContext(LogFields.UserId, userId)
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.ElapsedMs, elapsedMilliseconds)
               .ForContext(LogFields.MessageSource, "MediatR")
               .Warning("Long Running Request: {RequestName} took {ElapsedMs} ms. Threshold: {Threshold} ms.", requestName, elapsedMilliseconds, longRunningThresholdMs);
        }

        return response;
    }
}