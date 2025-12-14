using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Collections.Generic;
using System.Reflection;

namespace ProjectBase.Core.Extensions;

/// <summary>
/// Extension methods for configuring Swagger with JWT authentication and API versioning.
/// </summary>
public static class SwaggerExtension
{
    /// <summary>
    /// Adds Swagger generation with JWT authentication support and API versioning.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="projectName">Optional project name. If null, calling assembly name will be used.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services, string? projectName = null)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var assemblyName = projectName ?? Assembly.GetCallingAssembly().GetName().Name!;

        services.AddSwaggerGen(o =>
        {
            // Build temporary service provider to get IApiVersionDescriptionProvider
            // Note: This is necessary because Swagger configuration happens during service registration
            using var tempProvider = services.BuildServiceProvider();
            var apiVersionProvider = tempProvider.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in apiVersionProvider.ApiVersionDescriptions)
            {
                o.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = $"{assemblyName} {description.GroupName}",
                    Version = description.ApiVersion.ToString(),
                    Description = $"API methods for {assemblyName}, Environment: {env}"
                });
            }

            o.EnableAnnotations();

            o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            // Configure JWT Security Scheme
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            };
            o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

            // Add security requirement
            o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, doc, ReferenceType.SecurityScheme.ToString()),
                    new List<string>()
                }
            });
        });

        return services;
    }
}
