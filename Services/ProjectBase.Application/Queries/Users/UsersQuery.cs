using MediatR;
using ProjectBase.Core.Results;
using ProjectBase.Model.ResponseModels.Users;

namespace ProjectBase.Application.Queries.Users;

public record UsersQuery : IRequest<Result<IEnumerable<UserResponse>>>;
