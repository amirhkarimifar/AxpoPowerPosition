using System.Globalization;
using PowerPosition.Core.Domain;

namespace PowerPosition.Core.Reporting;

public sealed class CsvPositionReportWriter : IPositionReportWriter
{
    public async Task<string> WriteAsync(
        IReadOnlyList<PowerPositionRow> rows,
        string outputFolder,
        DateTimeOffset extractedAtLocal,
        CancellationToken cancellationToken)
    {
        var resolvedFolder = Path.GetFullPath(outputFolder);
        Directory.CreateDirectory(resolvedFolder);

        var fileName = $"PowerPosition_{extractedAtLocal.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture)}.csv";
        var finalPath = Path.Combine(resolvedFolder, fileName);
        var tempPath = Path.Combine(resolvedFolder, $"{fileName}.{Guid.NewGuid():N}.tmp");

        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        await using (var writer = new StreamWriter(stream))
        {
            await writer.WriteLineAsync("Local Time,Volume");

            foreach (var row in rows)
            {
                var localTime = row.LocalTime.ToString("HH:mm", CultureInfo.InvariantCulture);
                var volume = row.Volume.ToString("0.###############", CultureInfo.InvariantCulture);
                await writer.WriteLineAsync($"{localTime},{volume}".AsMemory(), cancellationToken);
            }
        }

        File.Move(tempPath, finalPath, overwrite: true);
        return finalPath;
    }
}
