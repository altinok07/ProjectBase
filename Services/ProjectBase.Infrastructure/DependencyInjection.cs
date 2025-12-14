using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        //Add Repositories

        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        return services;
    }
}
