using Microsoft.AspNetCore.Mvc;
using ProjectBase.Core.Results;
using ProjectBase.Core.Security.CurrentUser;

namespace ProjectBase.Core.Api.Controllers;


[Route("api/v{version:apiVersion}/{localizationCode}/[controller]")]
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected Guid? CurrentUserId
        => HttpContext?.Items.TryGetValue(CurrentUserConstants.HttpContextItemKey, out var value) == true
            && value is Guid id
            ? id
            : null;

    public string LocalizationCode { get; set; } = null!;

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
