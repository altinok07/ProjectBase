using Microsoft.Extensions.DependencyInjection;
using ProjectBase.Core.Localization.Interfaces;

namespace ProjectBase.Core.Localization;
public static class DependencyInjection
{
    public static IServiceCollection AddLocalizations(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ILocaleAccessor, HttpContextLocaleAccessor>();
        services.AddScoped<ILocalizedMessages, LocalizedMessages>();

        return services;
    }
}
