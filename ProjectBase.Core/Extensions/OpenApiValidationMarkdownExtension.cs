using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using ProjectBase.Core.Extensions;
using System.Collections.Concurrent;
using System.Reflection;

namespace ProjectBase.Core.Extensions;

public static class OpenApiValidationMarkdownExtension
{
    private static readonly ConcurrentDictionary<Type, string?> ValidationMarkdownCache = new();
    private const string ValidationHeader = "Doğrulama kuralları (FluentValidation):";

    /// <summary>
    /// FluentValidation kurallarını OpenAPI operation description alanına Markdown olarak ekler.
    /// Description doluysa, içeriğin altına ekler (intro + validation pattern).
    /// </summary>
    public static IServiceCollection AddOpenApiValidationMarkdown(this IServiceCollection services, string[] openApiDocuments)
    {
        foreach (var documentName in openApiDocuments.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
        {
            services.AddOpenApi(documentName, options =>
            {
                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    if (context.Description?.ActionDescriptor is not ControllerActionDescriptor cad)
                        return Task.CompletedTask;

                    // Allow endpoint-level opt-out via docs convention:
                    // public const bool DisableValidationMarkdown = true;
                    if (IsValidationMarkdownDisabledByDocs(cad))
                        return Task.CompletedTask;

                    var requestType = TryGetBodyRequestType(cad.MethodInfo);
                    if (requestType is null)
                        return Task.CompletedTask;

                    var markdown = ValidationMarkdownCache.GetOrAdd(requestType, BuildMarkdownForRequestType);
                    if (string.IsNullOrWhiteSpace(markdown))
                        return Task.CompletedTask;

                    if (string.IsNullOrWhiteSpace(operation.Description))
                    {
                        operation.Description = markdown;
                        return Task.CompletedTask;
                    }

                    // Already appended? (avoid duplicates if transformers run more than once)
                    if (operation.Description.Contains(ValidationHeader, StringComparison.Ordinal))
                        return Task.CompletedTask;

                    operation.Description =
                        $"{operation.Description.TrimEnd()}{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}{Environment.NewLine}{markdown}";

                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }

    private static Type? TryGetBodyRequestType(MethodInfo methodInfo)
    {
        // Heuristic: Controllers here typically have single [FromBody] request DTO/Command.
        // If no [FromBody], we skip (querystring primitives etc).
        foreach (var p in methodInfo.GetParameters())
        {
            var hasFromBody = p.GetCustomAttributes(inherit: true)
                .Any(a => string.Equals(a.GetType().Name, "FromBodyAttribute", StringComparison.Ordinal));

            if (!hasFromBody)
                continue;

            return p.ParameterType;
        }

        return null;
    }

    private static string? BuildMarkdownForRequestType(Type requestType)
    {
        var validator = TryCreateValidator(requestType);
        if (validator is null)
            return null;

        var descriptor = validator.CreateDescriptor();
        var members = descriptor.GetMembersWithValidators().OrderBy(x => x.Key).ToArray();
        if (members.Length == 0)
            return null;

        var lines = new List<string>
        {
            ValidationHeader,
            ""
        };

        foreach (var member in members)
        {
            var memberName = member.Key;
            var validators = member.Select(x => x.Validator).ToArray();
            if (validators.Length == 0)
                continue;

            var propertyType = requestType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.PropertyType;
            var typeText = propertyType is null ? "" : $" (`{ToCSharpTypeName(propertyType)}`)";

            var constraints = new List<string>();
            foreach (var v in validators)
            {
                switch (v)
                {
                    case INotEmptyValidator:
                    case INotNullValidator:
                        constraints.Add("zorunlu");
                        break;
                    case IMaximumLengthValidator max:
                        constraints.Add($"en fazla {max.Max} karakter");
                        break;
                    case IMinimumLengthValidator min:
                        constraints.Add($"en az {min.Min} karakter");
                        break;
                    case IEmailValidator:
                        constraints.Add("geçerli e-posta formatı");
                        break;
                }
            }

            constraints = constraints.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (constraints.Count == 0)
                continue;

            lines.Add($"- **{memberName}**{typeText}: {string.Join(", ", constraints)}");
        }

        return lines.Count > 2 ? string.Join(Environment.NewLine, lines) : null;
    }

    private static bool IsValidationMarkdownDisabledByDocs(ControllerActionDescriptor cad)
    {
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

            var disabled = endpointType
                .GetField("DisableValidationMarkdown", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) as bool?;

            return disabled is true;
        }

        return false;
    }

    private static IValidator? TryCreateValidator(Type requestType)
    {
        // Validator'lar genelde requestType ile aynı assembly'de.
        var assembly = requestType.Assembly;
        var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);

        var candidate = assembly
            .GetTypes()
            .FirstOrDefault(t =>
                t is { IsAbstract: false, IsInterface: false } &&
                validatorInterface.IsAssignableFrom(t) &&
                t.GetConstructor(Type.EmptyTypes) is not null);

        return candidate is null ? null : Activator.CreateInstance(candidate) as IValidator;
    }

    private static string ToCSharpTypeName(Type t)
    {
        if (t == typeof(string)) return "string";
        if (t == typeof(int)) return "int";
        if (t == typeof(long)) return "long";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(decimal)) return "decimal";
        if (t == typeof(double)) return "double";
        if (t == typeof(float)) return "float";
        if (t == typeof(Guid)) return "Guid";
        if (t == typeof(DateTime)) return "DateTime";

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            return $"{ToCSharpTypeName(Nullable.GetUnderlyingType(t)!)}?";

        return t.Name;
    }
}


