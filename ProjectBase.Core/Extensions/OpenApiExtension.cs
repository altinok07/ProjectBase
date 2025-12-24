using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

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

                // Ensure Scalar can offer an "Auth" input and automatically send the Authorization header
                // when using "Try it out" by advertising a Bearer security scheme in the OpenAPI document.
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.OrdinalIgnoreCase);

                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
                    };

                    // Set global security requirement so UI knows to attach Authorization by default.
                    // If you have anonymous endpoints, you can override this later per-operation.
                    document.Security = new List<OpenApiSecurityRequirement>
                    {
                        new()
                        {
                            [ new OpenApiSecuritySchemeReference("Bearer", document, externalResource: null) ] = []
                        }
                    };

                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }
}
