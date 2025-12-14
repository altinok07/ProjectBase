using ProjectBase.Domain.Repositories.Users;

namespace ProjectBase.Domain.Base;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }

    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
