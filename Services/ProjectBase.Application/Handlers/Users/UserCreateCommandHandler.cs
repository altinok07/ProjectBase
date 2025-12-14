using AutoMapper;
using MediatR;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Core.Results;
using ProjectBase.Core.Security;
using ProjectBase.Domain.Base;
using ProjectBase.Domain.Entities.Users;

namespace ProjectBase.Application.Handlers.Users;

internal sealed class UserCreateCommandHandler(IUnitOfWork repo, IMapper mapper, IHashProperty hashProperty) : IRequestHandler<UserCreateCommand, Result>
{
    private readonly IUnitOfWork _repo = repo;
    private readonly IMapper _mapper = mapper;
    private readonly IHashProperty _hashProperty = hashProperty;

    public async Task<Result> Handle(UserCreateCommand request, CancellationToken cancellationToken)
    {
        var userMapped = _mapper.Map<User>(request);
        userMapped.PasswordHash = _hashProperty.Hash(request.Password);

        var result = await _repo.UserRepository.AddAsync(userMapped);

        if (!result.IsSuccess)
            return Result.Fail(result.ResponseType, result.Errors!.FirstOrDefault()!.ErrorMessage);

        return Result.Success(result.ResponseType);
    }
}
