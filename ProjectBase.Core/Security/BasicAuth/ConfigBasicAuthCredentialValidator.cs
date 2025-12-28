using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProjectBase.Core.Security.BasicAuth;

public sealed class ConfigBasicAuthCredentialValidator(IOptions<BasicAuthSettings> options) : IBasicAuthCredentialValidator
{
    private readonly BasicAuthSettings _settings = options.Value;

    public Task<BasicAuthValidationResult?> ValidateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = _settings.User;
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            return Task.FromResult<BasicAuthValidationResult?>(null);

        if (!string.Equals(user.Username, username, StringComparison.Ordinal))
            return Task.FromResult<BasicAuthValidationResult?>(null);

        // NOTE: Bu sample config'te parola plaintext. Üretimde hash veya secret store önerilir.
        if (!FixedTimeEquals(user.Password, password))
            return Task.FromResult<BasicAuthValidationResult?>(null);

        List<Claim> claims =
        [
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("AuthType", "Basic")
        ];

        return Task.FromResult<BasicAuthValidationResult?>(new BasicAuthValidationResult(user.Username, claims));
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected ?? string.Empty);
        var actualBytes = Encoding.UTF8.GetBytes(actual ?? string.Empty);

        // FixedTimeEquals uzunluklar farklıysa false döndürür; ama timing farkını minimize etmek için
        // aynı uzunlukta buffer'a kopyalıyoruz.
        var max = Math.Max(expectedBytes.Length, actualBytes.Length);
        var a = new byte[max];
        var b = new byte[max];
        Buffer.BlockCopy(expectedBytes, 0, a, 0, expectedBytes.Length);
        Buffer.BlockCopy(actualBytes, 0, b, 0, actualBytes.Length);
        return CryptographicOperations.FixedTimeEquals(a, b) && expectedBytes.Length == actualBytes.Length;
    }
}


