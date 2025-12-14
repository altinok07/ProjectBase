using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectBase.Domain.Entities.Users;

namespace ProjectBase.Infrastructure.Configurations.Users;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(entity => entity.Id);

        #region Columns
        builder.Property(entity => entity.Name)
            .HasColumnOrder(1)
            .HasColumnType("nvarchar(256)")
            .IsRequired();

        builder.Property(entity => entity.Surname)
            .HasColumnOrder(2)
            .HasColumnType("nvarchar(256)")
            .IsRequired();

        builder.Property(entity => entity.Email)
            .HasColumnOrder(3)
            .HasColumnType("nvarchar(256)")
            .IsRequired();

        builder.Property(entity => entity.Phone)
            .HasColumnOrder(4)
            .HasColumnType("nvarchar(256)")
            .IsRequired();

        builder.Property(entity => entity.PasswordHash)
            .HasColumnOrder(5)
            .HasColumnType("nvarchar(256)")
            .IsRequired();
        #endregion
    }
}
