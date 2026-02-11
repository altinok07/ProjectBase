namespace ProjectBase.Core.Helpers;

/// <summary>
/// Türkiye timezone dönüşümleri için helper metodlar.
/// </summary>
public static class TimeZoneHelper
{
    private static readonly TimeZoneInfo TurkeyTimeZone = GetTurkeyTimeZone();

    /// <summary>
    /// Türkiye timezone bilgisini platform bağımsız olarak alır.
    /// Windows'ta "Turkey Standard Time", Linux/Mac'te "Europe/Istanbul" kullanır.
    /// </summary>
    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Turkey Time",
                    TimeSpan.FromHours(3),
                    "Turkey Time",
                    "Turkey Time");
            }
        }
    }

    /// <summary>
    /// Türkiye timezone bilgisini döner. Hangfire gibi zamanlama kütüphaneleri için kullanılabilir.
    /// </summary>
    public static TimeZoneInfo GetTurkeyTimeZoneInfo() => TurkeyTimeZone;

    /// <summary>
    /// DateTime değerini Türkiye saatine çevirir.
    /// </summary>
    public static DateTime ToTurkeyTime(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, TurkeyTimeZone);
        
        if (dateTime.Kind == DateTimeKind.Local)
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), TurkeyTimeZone);
        
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), TurkeyTimeZone);
    }

    /// <summary>
    /// DateTime değerini Türkiye saatine çevirir ve DateTimeOffset olarak döner.
    /// </summary>
    public static DateTimeOffset ToTurkeyOffset(this DateTime dateTime)
    {
        var turkeyTime = dateTime.ToTurkeyTime();
        return new DateTimeOffset(turkeyTime, TurkeyTimeZone.GetUtcOffset(turkeyTime));
    }
}

