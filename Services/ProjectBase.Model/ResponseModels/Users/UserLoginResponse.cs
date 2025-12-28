namespace ProjectBase.Model.ResponseModels.Users;

public class UserLoginResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Mail { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public DateTime AccessTokenExpires { get; set; }
    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshTokenExpires { get; set; }
}
