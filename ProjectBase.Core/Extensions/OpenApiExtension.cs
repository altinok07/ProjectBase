using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Reflection;

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

                // Convention-based endpoint docs:
                // If Summary/Description is not explicitly provided on the action, try to resolve it from a
                // docs class following a naming convention:
                //
                // Controller namespace: ProjectBase.Api.Controllers.v1
                // Docs namespace:       ProjectBase.Api.OpenApiDocs.v1
                //
                // Docs type candidates:
                // - {DocsNamespace}.{ControllerName}s.{ControllerName}OpenApiDocs   (preferred, plural folder)
                // - {DocsNamespace}.{ControllerName}.{ControllerName}OpenApiDocs    (singular folder)
                // - {DocsNamespace}.{ControllerName}OpenApiDocs                     (no folder)
                //
                // Nested type: {ActionName}
                // Fields: public const string Summary/Description
                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    // If both already set, don't do anything.
                    if (!string.IsNullOrWhiteSpace(operation.Summary) && !string.IsNullOrWhiteSpace(operation.Description))
                        return Task.CompletedTask;

                    if (context.Description?.ActionDescriptor is not ControllerActionDescriptor cad)
                        return Task.CompletedTask;

                    var controllerNamespace = cad.ControllerTypeInfo.Namespace ?? string.Empty;
                    var docsNamespace = controllerNamespace.Replace(".Controllers.", ".OpenApiDocs.");

                    var controllerName = cad.ControllerName; // e.g. "User"
                    var actionName = cad.ActionName;         // e.g. "Register"

                    var assembly = cad.ControllerTypeInfo.Assembly;

                    var typeCandidates = new[]
                    {
                        $"{docsNamespace}.{controllerName}s.{controllerName}OpenApiDocs",
                        $"{docsNamespace}.{controllerName}.{controllerName}OpenApiDocs",
                        $"{docsNamespace}.{controllerName}OpenApiDocs"
                    };

                    foreach (var typeName in typeCandidates)
                    {
                        var docsType = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
                        if (docsType is null)
                            continue;

                        var endpointType = docsType.GetNestedType(actionName, BindingFlags.Public);
                        if (endpointType is null)
                            continue;

                        var summary = endpointType.GetField("Summary", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string;
                        var description = endpointType.GetField("Description", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string;

                        if (string.IsNullOrWhiteSpace(operation.Summary) && !string.IsNullOrWhiteSpace(summary))
                            operation.Summary = summary;

                        if (string.IsNullOrWhiteSpace(operation.Description) && !string.IsNullOrWhiteSpace(description))
                            operation.Description = description;

                        break;
                    }

                    return Task.CompletedTask;
                });

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

                    document.Components.SecuritySchemes["Basic"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "basic",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Description = "Basic Authorization header. Example: \"Basic {base64(username:password)}\""
                    };

                    // Set global security requirement so UI knows to attach Authorization by default.
                    // If you have anonymous endpoints, you can override this later per-operation.
                    document.Security = new List<OpenApiSecurityRequirement>
                    {
                        new()
                        {
                            [ new OpenApiSecuritySchemeReference("Bearer", document, externalResource: null) ] = []
                        }
                        ,
                        new()
                        {
                            [ new OpenApiSecuritySchemeReference("Basic", document, externalResource: null) ] = []
                        }
                    };

                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }
}
