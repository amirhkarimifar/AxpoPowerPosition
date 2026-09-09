using Microsoft.Extensions.Options;
using PowerPosition.Core.Domain;
using PowerPosition.Core.Extraction;
using PowerPosition.Core.Options;

namespace PowerPosition.Worker;

public sealed class ExtractSchedulerService(
    IExtractRunner extractRunner,
    IOptionsMonitor<ExtractOptions> options,
    TimeProvider timeProvider,
    ILogger<ExtractSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupOptions = options.CurrentValue;
        var timeZone = TimeZoneResolver.Resolve(startupOptions.TimeZoneId);
        logger.LogInformation(
            "Extract schedule starting: OutputFolder={OutputFolder}, IntervalMinutes={IntervalMinutes}, TimeZoneId={TimeZoneId}, DayOffset={DayOffset}",
            Path.GetFullPath(startupOptions.OutputFolder), startupOptions.IntervalMinutes, startupOptions.TimeZoneId, startupOptions.DayOffset);

        var nextRun = timeProvider.GetUtcNow();

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            nextRun += TimeSpan.FromMinutes(options.CurrentValue.IntervalMinutes);
            var delay = nextRun - timeProvider.GetUtcNow();

            if (delay < TimeSpan.Zero)
            {
                logger.LogWarning(
                    "Extract run overran its scheduled interval by {Overrun:hh\\:mm\\:ss}; running the next extract immediately",
                    -delay);
                nextRun = timeProvider.GetUtcNow();
                delay = TimeSpan.Zero;
            }

            logger.LogInformation(
                "Worker idle; next extract due at {NextRunLocal:HH:mm:ss} (in {Delay:hh\\:mm\\:ss})",
                TimeZoneInfo.ConvertTime(nextRun, timeZone), delay);

            try
            {
                await Task.Delay(delay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Shutdown requested; extract schedule stopped cleanly");
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await extractRunner.RunAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Schedule continuing after a failed extract run ({Reason}); will retry at the next tick", ex.Message);
        }
    }
}
