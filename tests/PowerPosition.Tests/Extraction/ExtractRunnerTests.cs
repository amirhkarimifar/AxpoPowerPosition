using Axpo;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PowerPosition.Core.Aggregation;
using PowerPosition.Core.Extraction;
using PowerPosition.Core.Options;
using PowerPosition.Core.Reporting;
using PowerPosition.Tests.TestSupport;

namespace PowerPosition.Tests.Extraction;

public class ExtractRunnerTests : IDisposable
{
    private readonly string _outputFolder =
        Path.Combine(Path.GetTempPath(), "PowerPositionTests_" + Guid.NewGuid().ToString("N"));

    private readonly FakeTimeProvider _timeProvider =
        new(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));

    private ExtractRunner CreateRunner(FakePowerService powerService, int dayOffset = 1, IPositionReportWriter? writer = null)
    {
        var options = new TestOptionsMonitor<ExtractOptions>(new ExtractOptions
        {
            OutputFolder = _outputFolder,
            IntervalMinutes = 60,
            TimeZoneId = "Europe/London",
            DayOffset = dayOffset
        });

        return new ExtractRunner(
            powerService,
            new PowerPositionAggregator(),
            writer ?? new CsvPositionReportWriter(),
            ExtractResiliencePipeline.CreateForGetTrades(NullLogger.Instance),
            ExtractResiliencePipeline.CreateForWrite(NullLogger.Instance),
            _timeProvider,
            options,
            NullLogger<ExtractRunner>.Instance);
    }

    [Fact]
    public async Task SuccessfulRun_WritesOneFile_AndReturnsTheAggregatedRowCount()
    {
        var powerService = new FakePowerService(date => [PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(24, 100))]);
        var runner = CreateRunner(powerService);

        var result = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(24, result.RowCount);
        Assert.True(File.Exists(result.FilePath));
        Assert.Single(Directory.GetFiles(_outputFolder));
    }

    [Fact]
    public async Task DayOffset_ControlsWhichTradingDateIsRequested()
    {
        var powerService = new FakePowerService(date => [PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(24, 1))]);
        var runner = CreateRunner(powerService, dayOffset: 1);

        var result = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 1, 16), result.TradingDate);
    }

    [Fact]
    public async Task TransientFailures_AreRetried_AndStillProduceExactlyOneFile()
    {
        var powerService = new FakePowerService(date => [PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(24, 42))])
        {
            FailFirstNCalls = 3
        };
        var runner = CreateRunner(powerService);

        var result = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(4, powerService.CallCount);
        Assert.Single(Directory.GetFiles(_outputFolder));
        Assert.Equal(24, result.RowCount);
    }

    [Fact]
    public async Task FailuresBeyondTheRetryBudget_PropagateAndWriteNoFile()
    {
        var powerService = new FakePowerService(date => [PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(24, 1))])
        {
            FailFirstNCalls = 100
        };
        var runner = CreateRunner(powerService);

        await Assert.ThrowsAsync<PowerServiceException>(() => runner.RunAsync(CancellationToken.None));

        Assert.False(Directory.Exists(_outputFolder) && Directory.GetFiles(_outputFolder).Length > 0);
    }

    [Fact]
    public async Task TransientWriteFailures_AreRetried_AndStillProduceExactlyOneFile()
    {
        var powerService = new FakePowerService(date => [PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(24, 42))]);
        var writer = new FakePositionReportWriter(new CsvPositionReportWriter()) { FailFirstNCalls = 2 };
        var runner = CreateRunner(powerService, writer: writer);

        var result = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(3, writer.CallCount);
        Assert.Single(Directory.GetFiles(_outputFolder));
        Assert.Equal(24, result.RowCount);
    }

    [Fact]
    public async Task WriteFailuresBeyondTheRetryBudget_Propagate()
    {
        var powerService = new FakePowerService(date => [PowerTradeFixtures.Trade(date, PowerTradeFixtures.Constant(24, 1))]);
        var writer = new FakePositionReportWriter(new CsvPositionReportWriter()) { FailFirstNCalls = 100 };
        var runner = CreateRunner(powerService, writer: writer);

        await Assert.ThrowsAsync<IOException>(() => runner.RunAsync(CancellationToken.None));

        Assert.False(Directory.Exists(_outputFolder) && Directory.GetFiles(_outputFolder).Length > 0);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputFolder))
        {
            Directory.Delete(_outputFolder, recursive: true);
        }
    }
}
