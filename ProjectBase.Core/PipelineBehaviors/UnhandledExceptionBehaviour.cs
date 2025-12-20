using MediatR;
using ProjectBase.Core.Extensions;
using ProjectBase.Core.Logging;
using Serilog;
using System.Diagnostics;

namespace ProjectBase.Core.PipelineBehaviors;

internal class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            Log.ForContext(LogFields.CorrelationId, Activity.Current?.GetOrCreateCorrelationId())
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.MessageSource, "MediatR")
               .Error(ex, "Unhandled Exception in Request {RequestName} {@Request}", requestName, request);

            throw;
        }
    }
}