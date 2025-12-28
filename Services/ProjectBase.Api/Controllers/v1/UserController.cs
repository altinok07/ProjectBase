using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Application.Queries.Users;
using ProjectBase.Core.Api.Controllers;
using ProjectBase.Core.Pagination;
using ProjectBase.Core.Security.BasicAuth;

namespace ProjectBase.Api.Controllers.v1;

[ApiVersion(1)]
public class UserController(IMediator mediator) : BaseController
{
    private readonly IMediator _mediator = mediator;

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("Register")]
    public async Task<IActionResult> Create([FromBody] UserCreateCommand request)
        => CreateActionResult(await _mediator.Send(request));

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] UserLoginQuery request)
        => CreateActionResult(await _mediator.Send(request));

    //[Authorize(AuthenticationSchemes = BasicAuthenticationDefaults.AuthenticationScheme)]
    //[HttpGet]
    //public async Task<IActionResult> GetAllUser()
    //{
    //    var result = await _mediator.Send(new UsersQuery());
    //    return CreateActionResult(result);
    //}

    [HttpGet("Paged")]
    public async Task<IActionResult> Paged([FromQuery] PaginationFilterQuery query)
    {
        var result = await _mediator.Send(new UsersPagedQuery { FilterQuery = query });
        return CreateActionResult(result);
    }
}
