using AutoMapper;
using MediatR;
using ProjectBase.Application.Queries.Users;
using ProjectBase.Core.Results;
using ProjectBase.Domain.Base;
using ProjectBase.Model.ResponseModels.Users;

namespace ProjectBase.Application.Handlers.Users;

internal sealed class UsersQueryHandler(IUnitOfWork repo, IMapper mapper) : IRequestHandler<UsersQuery, Result<IEnumerable<UserResponse>>>
{
    private readonly IUnitOfWork _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<IEnumerable<UserResponse>>> Handle(UsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _repo.UserRepository.GetAllAsync();

        var usersMapped = _mapper.Map<IEnumerable<UserResponse>>(users);

        return Result<IEnumerable<UserResponse>>.Success(ResultType.Success, usersMapped);
    }
}
