namespace ProjectBase.Core.Localization.Interfaces;

public interface ILocalizedMessages
{
    string Get<TEnum>(TEnum key, params object[] args) where TEnum : Enum;
}
