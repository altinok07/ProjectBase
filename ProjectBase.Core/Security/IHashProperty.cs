namespace ProjectBase.Core.Security;

public interface IHashProperty
{
    string Hash(string value);
    bool Verify(string value, string valueHash);
}
