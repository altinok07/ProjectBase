using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ProjectBase.Core.Logging;

internal static class UserIdExtensions
{
    public static string GetUserIdOrAnonymous(this HttpContext? httpContext)
        => httpContext?.User.GetUserIdOrAnonymous() ?? "anonymous";

    public static string GetUserIdOrAnonymous(this ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return "anonymous";

        var value =
            user.FindFirst("UserId")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return string.IsNullOrWhiteSpace(value) ? "anonymous" : value;
    }
}


