using ProjectBase.Core.Entities;

namespace ProjectBase.Domain.Entities.Users;

public class User : BaseEntity<Guid>
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Mail { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public int UserTypeId { get; set; }

    public virtual UserType UserType { get; set; } = null!;
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}
