using FluentValidation;
using MediatR;
using ProjectBase.Core.Logging;
using Serilog;
using System.Diagnostics;

namespace ProjectBase.Core.PipelineBehaviors;

internal class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(validators.Select(I => I.ValidateAsync(context, cancellationToken)));

            var failures = validationResults.SelectMany(I => I.Errors).Where(I => I != null).ToList();
            if (failures.Count != 0)
            {
                var requestName = typeof(TRequest).Name;

                Log.ForContext(LogFields.CorrelationId, Activity.Current?.GetOrCreateCorrelationId())
                   .ForContext(LogFields.RequestName, requestName)
                   .ForContext(LogFields.ResponseBody, failures.Select(I => new { I.PropertyName, I.ErrorMessage }), destructureObjects: true)
                   .ForContext(LogFields.MessageSource, "MediatR")
                   .Warning("Validation Failed for Request {RequestName}. {FailureCount} validation errors found.", requestName, failures.Count);

                throw new ValidationException(failures);
            }
        }

        return await next(cancellationToken);
    }
}