using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectBase.Domain.Entities.Users;
using ProjectBase.Domain.Enums;

namespace ProjectBase.Infrastructure.Configurations.Users;

public class UserTypeConfiguration : IEntityTypeConfiguration<UserType>
{
    public void Configure(EntityTypeBuilder<UserType> builder)
    {
        builder.ToTable("UserTypes");
        builder.HasKey(entity => entity.Id);

        #region Columns
        builder.Property(entity => entity.Id)
            .HasColumnOrder(1)
            .IsRequired();

        builder.Property(entity => entity.Name)
            .HasColumnOrder(1)
            .HasMaxLength(64)
            .IsRequired();
        #endregion

        #region SeedData
        builder.Property(entity => entity.CreatedDate).HasDefaultValueSql("GETDATE()");
        builder.Property(entity => entity.CreatedBy).HasDefaultValue("System");
        builder.Property(entity => entity.UpdatedDate).HasDefaultValueSql("GETDATE()");
        builder.Property(entity => entity.UpdatedBy).HasDefaultValue("System");
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false);

        builder.HasData(new UserType { Id = (int)UserTypeEnum.SystemUser, Name = nameof(UserTypeEnum.SystemUser) });
        builder.HasData(new UserType { Id = (int)UserTypeEnum.TenantUser, Name = nameof(UserTypeEnum.TenantUser) });
        #endregion
    }
}