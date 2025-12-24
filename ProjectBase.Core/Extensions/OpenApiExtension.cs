using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectBase.Core.Extensions;

public static class OpenApiExtension
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services, IConfiguration configuration, string[] openApiDocuments)
    {
        foreach (var documentName in openApiDocuments.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
        {
            services.AddOpenApi(documentName, options =>
            {
                options.ShouldInclude = api => string.Equals(api.GroupName, documentName, StringComparison.OrdinalIgnoreCase);
            });
        }

        return services;
    }
}
