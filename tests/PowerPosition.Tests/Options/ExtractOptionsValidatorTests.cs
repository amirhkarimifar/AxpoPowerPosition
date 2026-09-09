using PowerPosition.Core.Options;

namespace PowerPosition.Tests.Options;

public class ExtractOptionsValidatorTests : IDisposable
{
    private readonly string _validFolder =
        Path.Combine(Path.GetTempPath(), "PowerPositionTests_" + Guid.NewGuid().ToString("N"));

    private readonly ExtractOptionsValidator _validator = new();

    [Fact]
    public void ValidOptions_Pass()
    {
        var result = _validator.Validate(null, new ExtractOptions { OutputFolder = _validFolder, IntervalMinutes = 5 });
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void MissingOutputFolder_Fails()
    {
        var result = _validator.Validate(null, new ExtractOptions { OutputFolder = "", IntervalMinutes = 5 });
        Assert.True(result.Failed);
    }

    [Fact]
    public void NonPositiveInterval_Fails()
    {
        var result = _validator.Validate(null, new ExtractOptions { OutputFolder = _validFolder, IntervalMinutes = 0 });
        Assert.True(result.Failed);
    }

    [Fact]
    public void UnknownTimeZoneId_Fails()
    {
        var result = _validator.Validate(null, new ExtractOptions
        {
            OutputFolder = _validFolder,
            IntervalMinutes = 5,
            TimeZoneId = "Not/A_Real_Zone"
        });
        Assert.True(result.Failed);
    }

    public void Dispose()
    {
        if (Directory.Exists(_validFolder))
        {
            Directory.Delete(_validFolder, recursive: true);
        }
    }
}
