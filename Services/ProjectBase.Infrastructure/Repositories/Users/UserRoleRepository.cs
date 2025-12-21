using ProjectBase.Core.Repositories.EfCore;
using ProjectBase.Domain.Entities.Users;
using ProjectBase.Domain.Repositories.Users;

namespace ProjectBase.Infrastructure.Repositories.Users;

public class UserRoleRepository(ApplicationContext context) : Repository<UserRole>(context), IUserRoleRepository;
