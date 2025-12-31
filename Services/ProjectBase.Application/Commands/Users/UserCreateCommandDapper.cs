using MediatR;
using ProjectBase.Core.Results;

namespace ProjectBase.Application.Commands.Users;

public class UserCreateCommandDapper : IRequest<Result>
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Mail { get; set; } = null!;
    public string Password { get; set; } = null!;
}