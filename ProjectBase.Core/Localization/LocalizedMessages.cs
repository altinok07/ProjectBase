using ProjectBase.Core.Localization.Interfaces;

namespace ProjectBase.Core.Localization;

public class LocalizedMessages : ILocalizedMessages
{
    private const string DefaultCulture = "tr-tr";

    private readonly ILocaleAccessor _locale;
    private readonly Dictionary<string, Dictionary<Type, Dictionary<Enum, string>>> _catalog;

    // ICultureCatalog implementasyonlarını DI’dan alıp tek bir kataloğa MERGE ediyoruz.
    public LocalizedMessages(ILocaleAccessor locale, IEnumerable<ICultureCatalog> catalogs)
    {
        _locale = locale;
        _catalog = new(StringComparer.OrdinalIgnoreCase);

        foreach (var c in catalogs)
        {
            if (!_catalog.TryGetValue(c.Culture, out var byType))
                byType = _catalog[c.Culture] = new();

            foreach (var kvType in c.Map)
            {
                if (!byType.TryGetValue(kvType.Key, out var byKey))
                    byKey = byType[kvType.Key] = new();

                foreach (var kv in kvType.Value)
                    byKey[kv.Key] = kv.Value; // son gelen override eder
            }
        }
    }

    public string Get<TEnum>(TEnum key, params object[] args) where TEnum : Enum
    {
        var culture = (_locale.Code ?? DefaultCulture).ToLowerInvariant();
        var type = typeof(TEnum);

        // 1) aktif kültürde ara
        if (!_catalog.TryGetValue(culture, out var byType) ||
            !byType.TryGetValue(type, out var byKey) ||
            !byKey.TryGetValue(key, out var template))
        {
            // 2) default kültüre düş
            if (!_catalog.TryGetValue(DefaultCulture, out var trByType) ||
                !trByType.TryGetValue(type, out var trByKey) ||
                !trByKey.TryGetValue(key, out template))
            {
                template = key!.ToString()!;
            }
        }

        try 
        { 
            return args?.Length > 0 ? string.Format(template, args) : template; 
        }
        catch (FormatException) 
        { 
            return template; 
        }
    }
}