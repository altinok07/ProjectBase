using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectBase.Core.FileService.Interfaces;

namespace ProjectBase.Core.FileService
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddFileService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FtpSettings>(configuration.GetSection(nameof(FtpSettings)));
            services.AddScoped<IFileService, FileService>();
            return services;
        }
    }
}

