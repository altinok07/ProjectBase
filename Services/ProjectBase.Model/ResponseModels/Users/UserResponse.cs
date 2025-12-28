namespace ProjectBase.Model.ResponseModels.Users;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Mail { get; set; } = null!;
}
