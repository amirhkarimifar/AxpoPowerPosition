using Axpo;
using PowerPosition.Core.Domain;

namespace PowerPosition.Core.Aggregation;

public interface IPowerPositionAggregator
{
    IReadOnlyList<PowerPositionRow> Aggregate(TradingDay tradingDay, IEnumerable<PowerTrade> trades);
}
