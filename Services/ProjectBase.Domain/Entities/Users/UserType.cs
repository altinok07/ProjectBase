using ProjectBase.Core.Entities;

namespace ProjectBase.Domain.Entities.Users;

public class UserType : BaseEntity<int>
{
    public string Name { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = [];
}
