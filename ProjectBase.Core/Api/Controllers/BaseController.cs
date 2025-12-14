using Microsoft.AspNetCore.Mvc;
using ProjectBase.Core.Results;

namespace ProjectBase.Core.Api.Controllers;


[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public abstract class BaseController : ControllerBase
{
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
