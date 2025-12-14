using Microsoft.EntityFrameworkCore.Storage;
using ProjectBase.Domain.Base;
using ProjectBase.Domain.Repositories.Users;
using ProjectBase.Infrastructure.Repositories.Users;

namespace ProjectBase.Infrastructure.Base;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationContext _context;
    private IDbContextTransaction? _transaction;

    private readonly IUserRepository _userRepository = null!;

    public UnitOfWork(ApplicationContext context)
    {
        _context = context;
        _userRepository = new UserRepository(context);
    }

    public IUserRepository UserRepository => _userRepository;

    public async Task BeginTransactionAsync()
    {
        if (_transaction == null)
            _transaction = await _context.Database.BeginTransactionAsync();
    }
    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _context.SaveChangesAsync(); // Commit öncesi DB kaydet
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
