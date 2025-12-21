using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectBase.Domain.Entities.Users;

namespace ProjectBase.Infrastructure.Configurations.Users;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(entity => new { entity.UserId, entity.RoleId });

        #region Columns
        builder.Property(entity => entity.UserId)
            .HasColumnOrder(1)
            .IsRequired();

        builder.Property(entity => entity.RoleId)
            .HasColumnOrder(2)
            .IsRequired();
        #endregion

        #region Relations
        // User ilişkisi
        builder.HasOne(userRole => userRole.User)
              .WithMany(user => user.UserRoles)
              .HasForeignKey(userRole => userRole.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Role ilişkisi
        builder.HasOne(userRole => userRole.Role)
        .WithMany(role => role.UserRoles)
        .HasForeignKey(userRole => userRole.RoleId)
        .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}