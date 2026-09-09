using Axpo;
using PowerPosition.Core.Domain;

namespace PowerPosition.Core.Aggregation;

public sealed class PowerPositionAggregator : IPowerPositionAggregator
{
    public IReadOnlyList<PowerPositionRow> Aggregate(TradingDay tradingDay, IEnumerable<PowerTrade> trades)
    {
        var totals = new SortedDictionary<int, double>();

        foreach (var trade in trades)
        {
            foreach (var period in trade.Periods)
            {
                totals.TryGetValue(period.Period, out var runningVolume);
                totals[period.Period] = runningVolume + period.Volume;
            }
        }

        return totals
            .Select(entry => new PowerPositionRow(entry.Key, tradingDay.PeriodLocalTime(entry.Key), entry.Value))
            .ToList();
    }
}
