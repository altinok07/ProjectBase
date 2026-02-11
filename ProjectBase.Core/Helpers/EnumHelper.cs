using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ProjectBase.Core.Helpers;

public static class EnumHelper
{
    public static string GetDisplayName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var member = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();

        return display?.GetName() ?? value.ToString();
    }
}




