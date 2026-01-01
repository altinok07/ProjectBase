using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectBase.Domain.Entities.Users;
using ProjectBase.Domain.Enums;

namespace ProjectBase.Infrastructure.Configurations.Users;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(entity => entity.Id);

        #region Columns
        builder.Property(entity => entity.Id)
            .HasColumnOrder(1)
            .IsRequired();

        builder.Property(entity => entity.Name)
            .HasColumnOrder(2)
            .HasMaxLength(64)
            .IsRequired();
        #endregion

        #region SeedData
        builder.Property(entity => entity.CreatedDate).HasDefaultValueSql("GETDATE()");
        builder.Property(entity => entity.CreatedBy).HasDefaultValue("System");
        builder.Property(entity => entity.UpdatedDate).HasDefaultValueSql("GETDATE()");
        builder.Property(entity => entity.UpdatedBy).HasDefaultValue("System");
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false);

        builder.HasData(new Role { Id = (int)UserRoleEnum.SystemAdmin, Name = nameof(UserRoleEnum.SystemAdmin) });
        builder.HasData(new Role { Id = (int)UserRoleEnum.TenantAdmin, Name = nameof(UserRoleEnum.TenantAdmin) });
        #endregion
    }
}
