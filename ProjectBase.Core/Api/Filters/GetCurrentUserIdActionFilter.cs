using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectBase.Core.Results;
using ProjectBase.Core.Security.CurrentUser;
using System.Security.Claims;

namespace ProjectBase.Core.Api.Filters;

public sealed class GetCurrentUserIdActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        // Only enforce when request uses Bearer auth (Basic or anonymous endpoints shouldn't be blocked).
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        var isBearer = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (isBearer)
        {
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = Unauthorized("Geçerli bir token sağlayın");
                return;
            }

            var raw = user.FindFirstValue("UserId");

            if (!Guid.TryParse(raw, out var userId))
            {
                context.Result = Unauthorized("Token içinde geçerli bir kullanıcı kimliği (UserId) bulunamadı.");
                return;
            }

            httpContext.Items[CurrentUserConstants.HttpContextItemKey] = userId;
        }

        await next();
    }

    private static ObjectResult Unauthorized(string message)
    {
        var payload = Result.Fail(ResultType.Unauthorized, message);
        return new ObjectResult(payload) { StatusCode = StatusCodes.Status401Unauthorized };
    }
}


