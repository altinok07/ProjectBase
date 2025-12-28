namespace ProjectBase.Core.Security.Jwt;

public sealed record JwtTokenPair(
    string AccessToken,
    DateTime AccessTokenExpires,
    string RefreshToken,
    DateTime RefreshTokenExpires
);


