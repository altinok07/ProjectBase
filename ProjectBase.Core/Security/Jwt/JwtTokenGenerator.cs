using Microsoft.IdentityModel.Tokens;
using ProjectBase.Core.Security.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectBase.Core.Security.Jwt;

public sealed class JwtTokenGenerator(ITokenSettings tokenSettings) : IJwtTokenGenerator
{
    private readonly ITokenSettings _tokenSettings = tokenSettings ?? throw new ArgumentNullException(nameof(tokenSettings));

    public JwtTokenPair GenerateTokenPair(IEnumerable<Claim> claims)
    {
        if (claims is null) throw new ArgumentNullException(nameof(claims));

        var nowUtc = DateTime.UtcNow;

        var accessExpires = nowUtc.AddMinutes(_tokenSettings.AccessTokenExpiration);
        var accessToken = CreateJwt(
            _tokenSettings.AccessTokenSecurityKey,
            _tokenSettings.Issuer,
            _tokenSettings.Audience,
            claims,
            nowUtc,
            accessExpires
        );

        var refreshExpires = nowUtc.AddMinutes(_tokenSettings.RefreshTokenExpiration);
        // Refresh token'ı da JWT olarak üretiyoruz (ayrı key ve expiry ile).
        // İsterseniz bunu random string + DB saklama modeline çevirebiliriz.
        var refreshClaims = EnsureJti(claims);
        var refreshToken = CreateJwt(
            _tokenSettings.RefreshTokenSecurityKey,
            _tokenSettings.Issuer,
            _tokenSettings.Audience,
            refreshClaims,
            nowUtc,
            refreshExpires
        );

        return new JwtTokenPair(accessToken, accessExpires, refreshToken, refreshExpires);
    }

    private static string CreateJwt(
        string securityKey,
        string issuer,
        string audience,
        IEnumerable<Claim> claims,
        DateTime notBeforeUtc,
        DateTime expiresUtc)
    {
        if (string.IsNullOrWhiteSpace(securityKey)) throw new ArgumentException("Security key cannot be empty.", nameof(securityKey));
        if (string.IsNullOrWhiteSpace(issuer)) throw new ArgumentException("Issuer cannot be empty.", nameof(issuer));
        if (string.IsNullOrWhiteSpace(audience)) throw new ArgumentException("Audience cannot be empty.", nameof(audience));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: EnsureJti(claims),
            notBefore: notBeforeUtc,
            expires: expiresUtc,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static IEnumerable<Claim> EnsureJti(IEnumerable<Claim> claims)
    {
        var list = claims as IList<Claim> ?? claims.ToList();
        if (list.Any(c => c.Type == JwtRegisteredClaimNames.Jti))
            return list;

        list.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));
        return list;
    }
}


