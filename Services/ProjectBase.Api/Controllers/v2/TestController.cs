using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using ProjectBase.Core.Api.Controllers;

namespace ProjectBase.Api.Controllers.v2;

[ApiVersion(2)]
public class TestController : BaseController
{

    [HttpGet]
    public async Task<IActionResult> adasd([FromQuery] string test)
    {
        return Ok(test);
    }
}
