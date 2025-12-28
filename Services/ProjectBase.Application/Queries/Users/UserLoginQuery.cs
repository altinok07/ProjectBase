using MediatR;
using ProjectBase.Core.Results;
using ProjectBase.Model.ResponseModels.Users;

namespace ProjectBase.Application.Queries.Users;

public class UserLoginQuery : IRequest<Result<UserLoginResponse>>
{
    public string Mail { get; set; } = null!;
    public string Password { get; set; } = null!;
}
