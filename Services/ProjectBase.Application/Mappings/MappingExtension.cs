using Microsoft.Extensions.DependencyInjection;
using ProjectBase.Application.Mappings.Users;

namespace ProjectBase.Application.Mappings;

public static class MappingExtension
{
    public static IServiceCollection AddMappingProfiles(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(UserProfile));

        return services;
    }
}