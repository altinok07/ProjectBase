using Microsoft.AspNetCore.Builder;
using ProjectBase.Core.Middlewares;

namespace ProjectBase.Core.Extensions;

public static class MiddlewareExtension
{
    public static IApplicationBuilder UseMiddlewares(this IApplicationBuilder builder)
        => builder
            .UseMiddleware<HttpLoggingMiddleware>()
            .UseMiddleware<ExceptionHandlingMiddleware>();
}