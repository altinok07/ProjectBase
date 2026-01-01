using Microsoft.Extensions.DependencyInjection;

namespace ProjectBase.Core.Http;

public static class DependencyInjection
{
    public static IServiceCollection AddHttpResultClient(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<IHttpResultClient, HttpResultClient>();
        return services;
    }
}


