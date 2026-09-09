using Axpo;

namespace PowerPosition.Tests.TestSupport;

public sealed class FakePowerService(Func<DateTime, IEnumerable<PowerTrade>> tradesForDate) : IPowerService
{
    private int _callCount;

    public int FailFirstNCalls { get; init; }

    public int CallCount => _callCount;

    public IEnumerable<PowerTrade> GetTrades(DateTime date) => Fetch(date);

    public Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date) => Task.FromResult(Fetch(date));

    private IEnumerable<PowerTrade> Fetch(DateTime date)
    {
        _callCount++;
        if (_callCount <= FailFirstNCalls)
        {
            throw new PowerServiceException("simulated failure");
        }

        return tradesForDate(date);
    }
}
