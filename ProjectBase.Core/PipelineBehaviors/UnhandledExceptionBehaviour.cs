using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ProjectBase.Core.Helpers;
using ProjectBase.Core.Logging;
using ProjectBase.Core.Logging.Models;
using Serilog;
using System.Diagnostics;

namespace ProjectBase.Core.PipelineBehaviors;

internal class UnhandledExceptionBehaviour<TRequest, TResponse>(IHttpContextAccessor httpContextAccessor, IOptions<HttpLoggingOptions> options) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
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
            var userId = httpContextAccessor.HttpContext.GetUserIdOrAnonymous();
            var correlationId = Activity.Current?.GetOrCreateCorrelationId();
            var requestJson = SerializeJsonSafe(request, options.Value);

            Log.ForContext(LogFields.CorrelationId, correlationId)
               .ForContext(LogFields.UserId, userId)
               .ForContext(LogFields.RequestName, requestName)
               .ForContext(LogFields.RequestBody, requestJson, destructureObjects: false)
               .ForContext(LogFields.MessageSource, "MediatR")
               .Error(ex, "Unhandled Exception in {request.name} (CorrelationId: {correlation.id})");

            throw;
        }
    }

    private static string SerializeJsonSafe(object? obj, HttpLoggingOptions optionsValue)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            json = MaskHelper.MaybeMask(json, optionsValue);

            if (string.IsNullOrEmpty(json) || json.Length <= optionsValue.MaxBodyLength)
                return json;

            return string.Concat(json.AsSpan(0, optionsValue.MaxBodyLength), "...(truncated)");
        }
        catch
        {
            return obj?.ToString() ?? "(null)";
        }
    }
}