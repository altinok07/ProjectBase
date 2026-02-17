using MediatR;
using ProjectBase.Core.Results;
using ProjectBase.Domain.Base;

namespace ProjectBase.Application.Handlers;

public class UserUpdateCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Mail { get; set; }
}


public class UserUpdateCommandHandler : IRequestHandler<UserUpdateCommand, Result>
{
    private readonly IUnitOfWork _repo;

    public UserUpdateCommandHandler(IUnitOfWork repo)
    {
        _repo = repo;
    }

    public async Task<Result> Handle(UserUpdateCommand request, CancellationToken cancellationToken)
    {
        var user = await _repo.UserRepository.GetAsync(new(I => I.Id == request.UserId));


        // Tek property güncelleme
        var updated = await _repo.UserRepository.UpdateAsync(
            x => x.Id == user.Id,
            s => s.SetProperty(e => e.Name, request.Name));


        // Birden fazla property güncelleme
        var updated2 = await _repo.UserRepository.UpdateAsync(
            x => x.Id == user.Id,
            s =>
            {
                s.SetProperty(e => e.Name, request.Name);
                s.SetProperty(e => e.Mail, request.Mail);
            });


        return Result.Success(ResultType.Success);
    }
}