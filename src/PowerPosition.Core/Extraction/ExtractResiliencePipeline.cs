using Axpo;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace PowerPosition.Core.Extraction;

public static class ExtractResiliencePipeline
{
    public static ResiliencePipeline CreateForGetTrades(ILogger logger) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PowerServiceException>(),
                MaxRetryAttempts = 5,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "GetTrades attempt {AttemptNumber} failed ({Reason}), retrying in {RetryDelay:ss\\.fff}s",
                        args.AttemptNumber + 1, args.Outcome.Exception?.Message, args.RetryDelay);
                    return default;
                }
            })
            .Build();

    public static ResiliencePipeline CreateForWrite(ILogger logger) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<IOException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(100),
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "CSV write attempt {AttemptNumber} failed ({Reason}), retrying in {RetryDelay:ss\\.fff}s",
                        args.AttemptNumber + 1, args.Outcome.Exception?.Message, args.RetryDelay);
                    return default;
                }
            })
            .Build();
}
