using System.Security.Cryptography;

namespace ProjectBase.Core.Security.Hashing;

public class HashProperty : IHashProperty
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;
    private readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(value, salt, Iterations, Algorithm, HashSize);
        return $"{Convert.ToHexString(hash)}-{Convert.ToHexString(salt)}";
    }

    public bool Verify(string value, string valueHash)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(valueHash))
            return false;

        string[] parts = valueHash.Split('-');
        if (parts.Length != 2)
            return false;

        try
        {
            byte[] hash = Convert.FromHexString(parts[0]);
            byte[] salt = Convert.FromHexString(parts[1]);

            byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(value, salt, Iterations, Algorithm, HashSize);

            return CryptographicOperations.FixedTimeEquals(hash, inputHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
