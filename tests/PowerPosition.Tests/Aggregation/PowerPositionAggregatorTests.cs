using PowerPosition.Core.Aggregation;
using PowerPosition.Core.Domain;
using PowerPosition.Tests.TestSupport;

namespace PowerPosition.Tests.Aggregation;

public class PowerPositionAggregatorTests
{
    private static readonly TimeZoneInfo London = TimeZoneResolver.Resolve("Europe/London");
    private readonly PowerPositionAggregator _aggregator = new();

    [Fact]
    public void MatchesTheReadmeWorkedExample()
    {
        var date = new DateTime(2026, 1, 15);
        var tradingDay = new TradingDay(DateOnly.FromDateTime(date), London);

        var trade1 = PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(24, 100));
        var trade2Volumes = PowerTradeFixtures.Constant(11, 50).Concat(PowerTradeFixtures.Constant(13, -20)).ToArray();
        var trade2 = PowerTradeFixtures.Trade(date, trade2Volumes);

        var rows = _aggregator.Aggregate(tradingDay, [trade1, trade2]);

        Assert.Equal(24, rows.Count);
        Assert.All(rows.Take(11), row => Assert.Equal(150, row.Volume));
        Assert.All(rows.Skip(11), row => Assert.Equal(80, row.Volume));
        Assert.Equal(new TimeOnly(23, 0), rows[0].LocalTime);
        Assert.Equal(new TimeOnly(9, 0), rows[10].LocalTime);
        Assert.Equal(new TimeOnly(10, 0), rows[11].LocalTime);
        Assert.Equal(new TimeOnly(22, 0), rows[23].LocalTime);
    }

    [Fact]
    public void NoTrades_ProducesNoRows()
    {
        var tradingDay = new TradingDay(new DateOnly(2026, 1, 15), London);

        var rows = _aggregator.Aggregate(tradingDay, []);

        Assert.Empty(rows);
    }

    [Fact]
    public void RowsAreOrderedByPeriod_RegardlessOfTradeOrInputOrder()
    {
        var date = new DateTime(2026, 1, 15);
        var tradingDay = new TradingDay(DateOnly.FromDateTime(date), London);
        var trade = PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(24, 10));

        var rows = _aggregator.Aggregate(tradingDay, [trade]);

        Assert.Equal(Enumerable.Range(1, 24), rows.Select(r => r.Period));
    }

    [Fact]
    public void FallBackDay_ProducesTwoSeparateRowsForTheDuplicatedLocalHour()
    {
        var date = new DateTime(2026, 10, 25);
        var tradingDay = new TradingDay(DateOnly.FromDateTime(date), London);
        var trade = PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(25, 5));

        var rows = _aggregator.Aggregate(tradingDay, [trade]);

        Assert.Equal(25, rows.Count);
        var duplicated = rows.Where(r => r.LocalTime == new TimeOnly(1, 0)).ToList();
        Assert.Equal(2, duplicated.Count);
        Assert.Equal([3, 4], duplicated.Select(r => r.Period));
    }
}
