using System.Globalization;
using PowerPosition.Core.Domain;
using PowerPosition.Core.Reporting;

namespace PowerPosition.Tests.Reporting;

public class CsvPositionReportWriterTests : IDisposable
{
    private readonly string _outputFolder;
    private readonly CsvPositionReportWriter _writer = new();

    public CsvPositionReportWriterTests()
    {
        _outputFolder = Path.Combine(Path.GetTempPath(), "PowerPositionTests_" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task WritesHeaderAndFilenameInTheSpecifiedFormat()
    {
        var rows = new[] { new PowerPositionRow(1, new TimeOnly(23, 0), 150) };
        var extractedAt = new DateTimeOffset(2014, 12, 20, 18, 37, 0, TimeSpan.Zero);

        var filePath = await _writer.WriteAsync(rows, _outputFolder, extractedAt, CancellationToken.None);

        Assert.Equal("PowerPosition_20141220_1837.csv", Path.GetFileName(filePath));
        var lines = await File.ReadAllLinesAsync(filePath);
        Assert.Equal("Local Time,Volume", lines[0]);
        Assert.Equal("23:00,150", lines[1]);
    }

    [Fact]
    public async Task DoesNotLeaveATempFileBehindAfterASuccessfulWrite()
    {
        var rows = new[] { new PowerPositionRow(1, new TimeOnly(23, 0), 150) };

        await _writer.WriteAsync(rows, _outputFolder, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.DoesNotContain(Directory.GetFiles(_outputFolder), f => f.EndsWith(".tmp"));
    }

    [Fact]
    public async Task VolumesAreFormattedWithInvariantCulture_NotTheAmbientOne()
    {
        var rows = new[] { new PowerPositionRow(1, new TimeOnly(23, 0), 150.5) };
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        try
        {
            var filePath = await _writer.WriteAsync(rows, _outputFolder, DateTimeOffset.UtcNow, CancellationToken.None);
            var content = await File.ReadAllTextAsync(filePath);

            Assert.Contains("23:00,150.5", content);
            Assert.DoesNotContain("150,5", content);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputFolder))
        {
            Directory.Delete(_outputFolder, recursive: true);
        }
    }
}
