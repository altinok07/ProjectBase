using MediatR;
using ProjectBase.Core.Results;

namespace ProjectBase.Application.Commands.Users;

public class UserCreateCommand : IRequest<Result>
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Password { get; set; } = null!;
}
