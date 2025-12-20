using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProjectBase.Core.PipelineBehaviors;

namespace ProjectBase.Core.Extensions;

public static class PipelineBehaviorExtension
{
    public static IServiceCollection AddPipelineBehaviors(this IServiceCollection services) => services
        .AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>))
        .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>))
        .AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>))
        .AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
}
