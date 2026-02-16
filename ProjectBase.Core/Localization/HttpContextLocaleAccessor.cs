using Microsoft.AspNetCore.Http;
using ProjectBase.Core.Localization.Interfaces;

namespace ProjectBase.Core.Localization;

public class HttpContextLocaleAccessor : ILocaleAccessor
{
    private readonly IHttpContextAccessor _http;
    public HttpContextLocaleAccessor(IHttpContextAccessor http) => _http = http;

    public string Code
    {
        get
        {
            var ctx = _http.HttpContext;
            var code = (ctx?.Items["localizationCode"]
                     ?? ctx?.Request.RouteValues["localizationCode"]
                     ?? "tr-tr")?.ToString();
            return string.IsNullOrWhiteSpace(code) ? "tr-tr" : code!.ToLowerInvariant();
        }
    }
}
