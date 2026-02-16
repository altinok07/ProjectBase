namespace ProjectBase.Core.Localization.Interfaces;

public interface ICultureCatalog
{
    string Culture { get; }
    // enumType -> (enumValue -> text)
    IReadOnlyDictionary<Type, IReadOnlyDictionary<Enum, string>> Map { get; }
}
