namespace PowerPosition.Core.Domain;

public sealed class TradingDay
{
    private readonly TimeZoneInfo _timeZone;

    public TradingDay(DateOnly date, TimeZoneInfo timeZone)
    {
        Date = date;
        _timeZone = timeZone;

        var previousDay = date.AddDays(-1);
        var localStart = new DateTime(previousDay.Year, previousDay.Month, previousDay.Day, 23, 0, 0, DateTimeKind.Unspecified);
        DayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
    }

    public DateOnly Date { get; }

    public DateTimeOffset DayStartUtc { get; }

    public DateTimeOffset PeriodStartUtc(int period) => DayStartUtc.AddHours(period - 1);

    public TimeOnly PeriodLocalTime(int period)
    {
        var local = TimeZoneInfo.ConvertTime(PeriodStartUtc(period), _timeZone);
        return TimeOnly.FromDateTime(local.DateTime);
    }

    public int ExpectedPeriodCount()
    {
        var nextDay = new TradingDay(Date.AddDays(1), _timeZone);
        return (int)(nextDay.DayStartUtc - DayStartUtc).TotalHours;
    }
}
