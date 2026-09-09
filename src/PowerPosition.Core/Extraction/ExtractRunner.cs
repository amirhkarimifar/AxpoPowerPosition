using System.Diagnostics;
using Axpo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using PowerPosition.Core.Aggregation;
using PowerPosition.Core.Domain;
using PowerPosition.Core.Options;
using PowerPosition.Core.Reporting;

namespace PowerPosition.Core.Extraction;

public sealed class ExtractRunner(
    IPowerService powerService,
    IPowerPositionAggregator aggregator,
    IPositionReportWriter writer,
    [FromKeyedServices(ExtractRunner.GetTradesPipelineKey)] ResiliencePipeline getTradesPipeline,
    [FromKeyedServices(ExtractRunner.WritePipelineKey)] ResiliencePipeline writePipeline,
    TimeProvider timeProvider,
    IOptionsMonitor<ExtractOptions> options,
    ILogger<ExtractRunner> logger) : IExtractRunner
{
    public const string GetTradesPipelineKey = "GetTrades";
    public const string WritePipelineKey = "Write";

    public async Task<ExtractResult> RunAsync(CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid();
        var runIdText = runId.ToString();
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["ExtractRunId"] = runIdText });

        var currentOptions = options.CurrentValue;
        var timeZone = TimeZoneResolver.Resolve(currentOptions.TimeZoneId);
        var nowLocal = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        var tradingDate = DateOnly.FromDateTime(nowLocal.DateTime.AddDays(currentOptions.DayOffset));
        var tradingDateText = tradingDate.ToString("yyyy-MM-dd");
        var tradingDay = new TradingDay(tradingDate, timeZone);

        logger.LogInformation("Starting extract for trading date {TradingDate}", tradingDateText);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var trades = await getTradesPipeline.ExecuteAsync(
                async ct => await powerService.GetTradesAsync(tradingDate.ToDateTime(TimeOnly.MinValue)).WaitAsync(ct),
                cancellationToken);

            var rows = aggregator.Aggregate(tradingDay, trades);

            var expectedPeriods = tradingDay.ExpectedPeriodCount();
            if (rows.Count != expectedPeriods)
            {
                logger.LogWarning(
                    "Expected {ExpectedPeriods} periods for {TradingDate} but aggregated {ActualPeriods}",
                    expectedPeriods, tradingDateText, rows.Count);
            }

            var filePath = await writePipeline.ExecuteAsync(
                async ct => await writer.WriteAsync(rows, currentOptions.OutputFolder, nowLocal, ct),
                cancellationToken);
            stopwatch.Stop();

            logger.LogInformation(
                "Extract {ExtractRunId} succeeded: {RowCount} rows written to {FilePath} in {ElapsedMs}ms",
                runIdText, rows.Count, filePath, stopwatch.ElapsedMilliseconds);

            return new ExtractResult(filePath, rows.Count, tradingDate, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogCritical(
                ex,
                "Extract {ExtractRunId} for {TradingDate} failed after all retries ({Reason}) — this scheduled slot was not written",
                runIdText, tradingDateText, ex.Message);
            throw;
        }
    }
}
