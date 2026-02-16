using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace ProjectBase.Core.Helpers;

public static class EnumHelper
{
    private static readonly ConcurrentDictionary<(Type EnumType, string MemberName), MemberInfo?> MemberCache = new();

    private static MemberInfo? GetMemberInfo(Type enumType, string memberName) =>
        MemberCache.GetOrAdd((enumType, memberName), k => k.EnumType.GetMember(k.MemberName).FirstOrDefault());

    public static TAttribute? GetAttribute<TEnum, TAttribute>(TEnum value)
        where TEnum : struct, Enum
        where TAttribute : Attribute
    {
        var name = Enum.GetName(value) ?? value.ToString();
        var member = GetMemberInfo(typeof(TEnum), name);
        return member?.GetCustomAttribute<TAttribute>();
    }

    public static string GetDisplayName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var display = GetAttribute<TEnum, DisplayAttribute>(value);

        return display?.GetName() ?? value.ToString();
    }

    public static string GetDisplayShortName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var display = GetAttribute<TEnum, DisplayAttribute>(value);
        return display?.GetShortName() ?? GetDisplayName(value);
    }

    public static string? GetDisplayDescription<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var display = GetAttribute<TEnum, DisplayAttribute>(value);
        return display?.GetDescription();
    }

    public static IReadOnlyList<TEnum> GetValues<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>();

    public static IReadOnlyList<string> GetNames<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetNames<TEnum>();

    public static IReadOnlyList<(TEnum Value, string DisplayName)> GetDisplayItems<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Select(v => (v, GetDisplayName(v)))
            .ToArray();

    public static bool IsDefined<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value);

    public static bool TryParse<TEnum>(string? text, out TEnum value, bool ignoreCase = true)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = default;
            return false;
        }

        return Enum.TryParse(text.Trim(), ignoreCase, out value);
    }

    public static bool TryParseDefined<TEnum>(string? text, out TEnum value, bool ignoreCase = true)
        where TEnum : struct, Enum
    {
        if (!TryParse(text, out value, ignoreCase))
            return false;

        return Enum.IsDefined(value);
    }

    public static int ToInt32<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Convert.ToInt32(value, CultureInfo.InvariantCulture);

    public static long ToInt64<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Convert.ToInt64(value, CultureInfo.InvariantCulture);
}




