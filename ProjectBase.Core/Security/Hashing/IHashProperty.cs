namespace ProjectBase.Core.Security.Hashing;

public interface IHashProperty
{
    string Hash(string value);
    bool Verify(string value, string valueHash);
}
