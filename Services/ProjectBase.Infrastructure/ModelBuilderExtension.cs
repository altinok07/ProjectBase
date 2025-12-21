using Microsoft.EntityFrameworkCore;
using ProjectBase.Infrastructure.Configurations.Users;

namespace ProjectBase.Infrastructure;

public static class ModelBuilderExtension
{
    public static void Configurations(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new UserTypeConfiguration());
        builder.ApplyConfiguration(new UserRoleConfiguration());
        builder.ApplyConfiguration(new RoleConfiguration());
    }
}