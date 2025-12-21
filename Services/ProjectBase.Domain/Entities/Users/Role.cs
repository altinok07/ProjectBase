using ProjectBase.Core.Entities;

namespace ProjectBase.Domain.Entities.Users;

public class Role : BaseEntity<int>
{
    public string Name { get; set; } = null!;
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}