using Axpo;
using PowerPosition.Core.Aggregation;
using PowerPosition.Core.Domain;

namespace PowerPosition.Tests.Integration;

public class PowerServiceDstIntegrationTests
{
    private static readonly TimeZoneInfo London = TimeZoneResolver.Resolve("Europe/London");
    private readonly IPowerService _powerService = new PowerService();
    private readonly PowerPositionAggregator _aggregator = new();

    [Fact]
    public async Task SpringForwardDay_AggregatesTo23Rows()
    {
        var rows = await AggregateWithRetry(new DateTime(2026, 3, 29));
        Assert.Equal(23, rows.Count);
    }

    [Fact]
    public async Task FallBackDay_AggregatesTo25RowsWithDuplicateLocalHour()
    {
        var rows = await AggregateWithRetry(new DateTime(2026, 10, 25));
        Assert.Equal(25, rows.Count);
        Assert.Equal(2, rows.Count(r => r.LocalTime == new TimeOnly(1, 0)));
    }

    private async Task<IReadOnlyList<PowerPositionRow>> AggregateWithRetry(DateTime date)
    {
        for (var attempt = 1; attempt <= 8; attempt++)
        {
            try
            {
                var trades = await _powerService.GetTradesAsync(date);
                var tradingDay = new TradingDay(DateOnly.FromDateTime(date), London);
                return _aggregator.Aggregate(tradingDay, trades);
            }
            catch (PowerServiceException) when (attempt < 8)
            {
            }
        }

        throw new InvalidOperationException("PowerService did not succeed within the retry budget.");
    }
}
