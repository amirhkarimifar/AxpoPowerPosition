using PowerPosition.Core.Domain;

namespace PowerPosition.Tests.Domain;

public class TradingDayTests
{
    private static readonly TimeZoneInfo London = TimeZoneResolver.Resolve("Europe/London");

    [Fact]
    public void NormalDay_Has24Periods_StartingAt23_00()
    {
        var tradingDay = new TradingDay(new DateOnly(2026, 1, 15), London);

        Assert.Equal(24, tradingDay.ExpectedPeriodCount());
        Assert.Equal(new TimeOnly(23, 0), tradingDay.PeriodLocalTime(1));
        Assert.Equal(new TimeOnly(0, 0), tradingDay.PeriodLocalTime(2));
        Assert.Equal(new TimeOnly(22, 0), tradingDay.PeriodLocalTime(24));
    }

    [Fact]
    public void SpringForwardDay_Has23Periods_AndSkipsLocal01_00()
    {
        var tradingDay = new TradingDay(new DateOnly(2026, 3, 29), London);

        Assert.Equal(23, tradingDay.ExpectedPeriodCount());

        var localTimes = Enumerable.Range(1, 23).Select(tradingDay.PeriodLocalTime).ToList();
        Assert.DoesNotContain(new TimeOnly(1, 0), localTimes);
        Assert.Equal(new TimeOnly(0, 0), localTimes[1]);
        Assert.Equal(new TimeOnly(2, 0), localTimes[2]);
        Assert.Equal(new TimeOnly(22, 0), localTimes[^1]);
    }

    [Fact]
    public void FallBackDay_Has25Periods_AndDuplicatesLocal01_00()
    {
        var tradingDay = new TradingDay(new DateOnly(2026, 10, 25), London);

        Assert.Equal(25, tradingDay.ExpectedPeriodCount());

        var localTimes = Enumerable.Range(1, 25).Select(tradingDay.PeriodLocalTime).ToList();
        Assert.Equal(new TimeOnly(1, 0), localTimes[2]);
        Assert.Equal(new TimeOnly(1, 0), localTimes[3]);
        Assert.Equal(2, localTimes.Count(t => t == new TimeOnly(1, 0)));
        Assert.Equal(new TimeOnly(22, 0), localTimes[^1]);
    }
}
