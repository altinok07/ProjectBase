using System.Security.Claims;

namespace ProjectBase.Core.Security.BasicAuth;

public interface IBasicAuthCredentialValidator
{
    Task<BasicAuthValidationResult?> ValidateAsync(string username, string password, CancellationToken cancellationToken);
}

public sealed record BasicAuthValidationResult(string Username, IReadOnlyList<Claim> Claims);


