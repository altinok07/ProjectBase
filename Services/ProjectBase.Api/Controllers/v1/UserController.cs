using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Application.Queries.Users;
using ProjectBase.Core.Api.Controllers;

namespace ProjectBase.Api.Controllers.v1;

[ApiVersion(1)]
public class UserController(IMediator mediator) : BaseController
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateCommand command)
    {
        var result = await _mediator.Send(command);
        return CreateActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUser()
    {
        var result = await _mediator.Send(new UsersQuery());
        return CreateActionResult(result);
    }
}
