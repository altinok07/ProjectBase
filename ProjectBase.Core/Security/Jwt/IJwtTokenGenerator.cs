using System.Security.Claims;

namespace ProjectBase.Core.Security.Jwt;

public interface IJwtTokenGenerator
{
    JwtTokenPair GenerateTokenPair(IEnumerable<Claim> claims);
}


