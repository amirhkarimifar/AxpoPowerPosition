using Axpo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using PowerPosition.Core.Aggregation;
using PowerPosition.Core.Extraction;
using PowerPosition.Core.Options;
using PowerPosition.Core.Reporting;

namespace PowerPosition.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPowerPositionCore(this IServiceCollection services)
    {
        services.AddOptions<ExtractOptions>()
            .BindConfiguration(ExtractOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ExtractOptions>, ExtractOptionsValidator>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<IPowerPositionAggregator, PowerPositionAggregator>();
        services.AddSingleton<IPositionReportWriter, CsvPositionReportWriter>();
        services.AddSingleton<IExtractRunner, ExtractRunner>();
        services.AddKeyedSingleton(
            ExtractRunner.GetTradesPipelineKey,
            (sp, _) => ExtractResiliencePipeline.CreateForGetTrades(sp.GetRequiredService<ILogger<ExtractRunner>>()));
        services.AddKeyedSingleton(
            ExtractRunner.WritePipelineKey,
            (sp, _) => ExtractResiliencePipeline.CreateForWrite(sp.GetRequiredService<ILogger<ExtractRunner>>()));

        return services;
    }
}
