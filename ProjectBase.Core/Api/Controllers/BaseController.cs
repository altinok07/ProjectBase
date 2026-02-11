using Microsoft.AspNetCore.Mvc;
using ProjectBase.Core.Results;
using ProjectBase.Core.Security.CurrentUser;

namespace ProjectBase.Core.Api.Controllers;


[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Current authenticated user's id resolved by <see cref="NabzApp.Api.Filters.GetCurrentUserIdActionFilter"/>.
    /// </summary>
    protected Guid? CurrentUserId
        => HttpContext?.Items.TryGetValue(CurrentUserConstants.HttpContextItemKey, out var value) == true
            && value is Guid id
            ? id
            : null;

    [NonAction]
    protected IActionResult CreateActionResult(Result result)
    {
        return StatusCode((int)result.ResponseType, result);
    }

    [NonAction]
    protected IActionResult CreateActionResult<T>(Result<T> result)
    {
        return StatusCode((int)result.ResponseType, result);
    }
}
