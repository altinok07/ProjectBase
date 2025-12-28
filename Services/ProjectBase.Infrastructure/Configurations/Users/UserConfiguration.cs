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
        builder.Property(entity => entity.Id)
            .HasColumnOrder(1)
            .IsRequired();

        builder.Property(entity => entity.Name)
            .HasColumnOrder(2)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.Surname)
            .HasColumnOrder(3)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.Mail)
            .HasColumnOrder(4)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.PasswordHash)
            .HasColumnOrder(5)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.UserTypeId)
            .HasColumnOrder(6)
            .IsRequired();
        #endregion

        #region Relations
        builder.HasOne(u => u.UserType)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.UserTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}