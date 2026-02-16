using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectBase.Core.Api.Controllers;
using ProjectBase.Core.Results;
using System.Text.RegularExpressions;

namespace ProjectBase.Core.Api.Filters;

public class LocalizationActionFilter : IAsyncActionFilter
{
    private const string Key = "localizationCode";
    private static readonly HashSet<string> Supported =
        new(StringComparer.OrdinalIgnoreCase) { "tr-tr", "en-en" };

    private static readonly Regex CodeRegex =
        new("^[a-z]{2}-[a-z]{2}$", RegexOptions.Compiled);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var raw = context.RouteData.Values[Key]?.ToString();
        var code = (raw ?? string.Empty).ToLowerInvariant();

        // Format kontrolü
        if (string.IsNullOrWhiteSpace(code) || !CodeRegex.IsMatch(code))
        {
            var fail = Result.Fail(ResultType.BadRequest, "LocalizationCode 5 karakter olmalı ve 'tr-tr' formatında verilmelidir.");

            context.Result = new ObjectResult(fail)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return;
        }

        if (!Supported.Contains(code))
        {
            string supportedList = string.Join(", ", Supported.Select(s => $"'{s}'"));

            var fail = Result.Fail(ResultType.BadRequest, $"Geçersiz dil formatı. Yalnızca bu dilleri kullanabilirsiniz: {supportedList}");

            context.Result = new ObjectResult(fail)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return;
        }


        // Tek kaynak
        context.HttpContext.Items[Key] = code;

        if (context.Controller is BaseController bc)
            bc.LocalizationCode = code;

        // BaseRequest'ten türeyen TÜM argümanları güncelle
        foreach (var kv in context.ActionArguments.ToList())
        {
            if (kv.Value is BaseRequest br)
                br.LocalizationCode = code;
        }

        // string parametre vs. için de argümana yazmak istiyorsanız:
        if (!context.ActionArguments.ContainsKey(Key))
            context.ActionArguments.Add(Key, code);
        else
            context.ActionArguments[Key] = code;

        await next();
    }
}

public class BaseRequest
{
    public string LocalizationCode { get; set; } = null!;
}