using ProjectBase.Core.Repositories.EfCore;
using ProjectBase.Domain.Entities.Users;

namespace ProjectBase.Domain.Repositories.Users;

public interface IUserRepository : IRepository<User>
{
}
