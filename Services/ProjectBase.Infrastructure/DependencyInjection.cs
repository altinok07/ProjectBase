using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectBase.Core.Http;
using ProjectBase.Core.Repositories;
using ProjectBase.Domain.Base;
using ProjectBase.Infrastructure.Base;

namespace ProjectBase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationContext>(options =>
        {
            var sqlConStr = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(sqlConStr);
        });

        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddTransient<IUnitOfWork, UnitOfWork>();

        services.AddRepositories<ApplicationContext>();
        services.AddHttpResultClient();

        return services;
    }
}
