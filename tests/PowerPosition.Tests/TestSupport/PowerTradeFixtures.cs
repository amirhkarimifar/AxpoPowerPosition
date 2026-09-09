using Axpo;

namespace PowerPosition.Tests.TestSupport;

public static class PowerTradeFixtures
{
    public static PowerTrade Trade(DateTime date, params double[] volumeByPeriod)
    {
        var trade = PowerTrade.Create(date, volumeByPeriod.Length);
        for (var i = 0; i < volumeByPeriod.Length; i++)
        {
            trade.Periods[i].SetVolume(volumeByPeriod[i]);
        }

        return trade;
    }

    public static double[] Constant(int periods, double volume) =>
        Enumerable.Repeat(volume, periods).ToArray();
}
