using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectBase.Application.Mappings;
using ProjectBase.Core.Security;
using ProjectBase.Infrastructure;

namespace ProjectBase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddMappingProfiles();

        services.AddSingleton<IHashProperty, HashProperty>();

        services.AddInfrastructure(configuration);

        return services;
    }
}
