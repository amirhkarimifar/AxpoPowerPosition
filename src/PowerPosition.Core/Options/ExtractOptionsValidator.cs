using Microsoft.Extensions.Options;
using PowerPosition.Core.Domain;

namespace PowerPosition.Core.Options;

public sealed class ExtractOptionsValidator : IValidateOptions<ExtractOptions>
{
    public ValidateOptionsResult Validate(string? name, ExtractOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OutputFolder))
        {
            return ValidateOptionsResult.Fail($"{ExtractOptions.SectionName}:OutputFolder must be set.");
        }

        if (options.IntervalMinutes < 1)
        {
            return ValidateOptionsResult.Fail($"{ExtractOptions.SectionName}:IntervalMinutes must be at least 1.");
        }

        try
        {
            Directory.CreateDirectory(options.OutputFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ValidateOptionsResult.Fail(
                $"{ExtractOptions.SectionName}:OutputFolder '{options.OutputFolder}' is not writable: {ex.Message}");
        }

        try
        {
            TimeZoneResolver.Resolve(options.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return ValidateOptionsResult.Fail(
                $"{ExtractOptions.SectionName}:TimeZoneId '{options.TimeZoneId}' could not be resolved on this platform.");
        }

        return ValidateOptionsResult.Success;
    }
}
