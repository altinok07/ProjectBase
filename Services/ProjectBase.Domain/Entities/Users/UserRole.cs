using ProjectBase.Core.Entities;

namespace ProjectBase.Domain.Entities.Users;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public int RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}