namespace ProjectBase.Core.Security.BasicAuth;

public sealed class BasicAuthSettings
{
    public BasicAuthUser User { get; set; } = new();
}

public sealed class BasicAuthUser
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


