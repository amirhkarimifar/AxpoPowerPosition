using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PowerPosition.Core.Extraction;
using PowerPosition.Core.Options;
using PowerPosition.Tests.TestSupport;
using PowerPosition.Worker;

namespace PowerPosition.Tests.Scheduling;

public class ExtractSchedulerServiceTests
{
    [Fact]
    public async Task RunsImmediatelyOnStart_ThenAtFixedIntervalsWithNoDrift()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var runner = new RecordingExtractRunner(timeProvider, advancePerRun: TimeSpan.Zero);
        var scheduler = CreateScheduler(runner, timeProvider, intervalMinutes: 5);

        await scheduler.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => runner.CallCount >= 1);
        Assert.Equal(1, runner.CallCount);

        timeProvider.Advance(TimeSpan.FromMinutes(5));
        await WaitUntilAsync(() => runner.CallCount >= 2);

        timeProvider.Advance(TimeSpan.FromMinutes(5));
        await WaitUntilAsync(() => runner.CallCount >= 3);

        await scheduler.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task WhenARunOverrunsTheInterval_TheNextRunStartsImmediately_WithoutOverlappingRuns()
    {
        var start = DateTimeOffset.UtcNow;
        var timeProvider = new FakeTimeProvider(start);
        var runner = new RecordingExtractRunner(timeProvider, advancePerRun: TimeSpan.FromSeconds(90));
        var scheduler = CreateScheduler(runner, timeProvider, intervalMinutes: 1);

        await scheduler.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => runner.CallCount >= 3);
        await scheduler.StopAsync(CancellationToken.None);

        Assert.True(timeProvider.GetUtcNow() >= start + TimeSpan.FromSeconds(270));
        Assert.Equal(0, runner.MaxObservedConcurrency);
    }

    private static ExtractSchedulerService CreateScheduler(
        RecordingExtractRunner runner, FakeTimeProvider timeProvider, int intervalMinutes)
    {
        var options = new TestOptionsMonitor<ExtractOptions>(new ExtractOptions
        {
            OutputFolder = "unused",
            IntervalMinutes = intervalMinutes
        });

        return new ExtractSchedulerService(runner, options, timeProvider, NullLogger<ExtractSchedulerService>.Instance);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class RecordingExtractRunner(FakeTimeProvider timeProvider, TimeSpan advancePerRun) : IExtractRunner
    {
        private int _callCount;
        private int _inFlight;

        public int CallCount => _callCount;

        public int MaxObservedConcurrency { get; private set; }

        public Task<ExtractResult> RunAsync(CancellationToken cancellationToken)
        {
            var concurrent = Interlocked.Increment(ref _inFlight);
            MaxObservedConcurrency = Math.Max(MaxObservedConcurrency, concurrent - 1);

            Interlocked.Increment(ref _callCount);
            if (advancePerRun > TimeSpan.Zero)
            {
                timeProvider.Advance(advancePerRun);
            }

            Interlocked.Decrement(ref _inFlight);
            return Task.FromResult(new ExtractResult("n/a", 0, DateOnly.MinValue, TimeSpan.Zero));
        }
    }
}
