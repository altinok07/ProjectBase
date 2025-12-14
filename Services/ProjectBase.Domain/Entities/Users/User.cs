using ProjectBase.Core.Entities;

namespace ProjectBase.Domain.Entities.Users;

public class User : BaseEntity<Guid>
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}
