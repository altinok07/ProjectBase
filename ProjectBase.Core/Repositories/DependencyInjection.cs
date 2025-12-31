using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectBase.Core.Repositories.Dapper;

namespace ProjectBase.Core.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IDbConnectionProvider, EfCoreDbConnectionProvider<TContext>>();
        services.AddScoped<IDapperExecutor, DapperExecutor>();

        return services;
    }
}
