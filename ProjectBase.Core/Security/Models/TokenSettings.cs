namespace ProjectBase.Core.Security.Models;

public class TokenSettings : ITokenSettings
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string ProviderKey { get; set; } = null!;

    public int AccessTokenExpiration { get; set; }
    public string AccessTokenSecurityKey { get; set; } = null!;

    public int RefreshTokenExpiration { get; set; }
    public string RefreshTokenSecurityKey { get; set; } = null!;
}

public interface ITokenSettings
{
    string Issuer { get; set; }
    string Audience { get; set; }
    string ProviderKey { get; set; }

    int AccessTokenExpiration { get; set; }
    string AccessTokenSecurityKey { get; set; }

    int RefreshTokenExpiration { get; set; }
    string RefreshTokenSecurityKey { get; set; }
}
