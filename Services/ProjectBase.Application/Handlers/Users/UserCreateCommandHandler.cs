using AutoMapper;
using MediatR;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Core.Results;
using ProjectBase.Core.Security.Hashing;
using ProjectBase.Domain.Base;
using ProjectBase.Domain.Entities.Users;
using ProjectBase.Domain.Enums;

namespace ProjectBase.Application.Handlers.Users;

internal sealed class UserCreateCommandHandler(IUnitOfWork repo, IMapper mapper, IHashProperty hashProperty) : IRequestHandler<UserCreateCommand, Result>
{
    private readonly IUnitOfWork _repo = repo;
    private readonly IMapper _mapper = mapper;
    private readonly IHashProperty _hashProperty = hashProperty;

    public async Task<Result> Handle(UserCreateCommand request, CancellationToken cancellationToken)
    {
        var userExist = await _repo.UserRepository.GetAsync(new(I => I.Mail == request.Mail && !I.IsDeleted));
        if (userExist != null)
            return Result.Fail(ResultType.Conflict, "Kullanýcý Sistemde kayýtlý");

        var mapped = _mapper.Map<User>(request);
        mapped.PasswordHash = _hashProperty.Hash(request.Password);

        mapped.UserRoles = new List<UserRole> { new() { RoleId = (int)UserRoleEnum.TenantAdmin } };

        var model = await _repo.UserRepository.AddAsync(mapped);

        if (!model.IsSuccess)
            return Result.Fail(ResultType.InternalServerError, "Kullanýcý Eklenirken bir hata oluþtu");

        return Result.Success(ResultType.Created, "Kullanýcý Kaydý baþarýlý");
    }
}
