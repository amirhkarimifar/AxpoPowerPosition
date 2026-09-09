namespace PowerPosition.Dashboard.Services;

public sealed record CsvFileInfo(string FileName, DateTimeOffset ModifiedUtc, long SizeBytes, int RowCount);

public sealed class OutputFileService(MonitorPaths paths)
{
    public IReadOnlyList<CsvFileInfo> ListFiles()
    {
        if (!Directory.Exists(paths.OutputFolder))
        {
            return [];
        }

        return new DirectoryInfo(paths.OutputFolder)
            .GetFiles("PowerPosition_*.csv")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f => new CsvFileInfo(f.Name, f.LastWriteTimeUtc, f.Length, CountDataRows(f.FullName)))
            .ToList();
    }

    public IReadOnlyList<(string LocalTime, string Volume)>? ReadRows(string fileName)
    {
        var path = Path.Combine(paths.OutputFolder, Path.GetFileName(fileName));
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return File.ReadLines(path)
                .Skip(1)
                .Select(line => line.Split(','))
                .Where(parts => parts.Length == 2)
                .Select(parts => (parts[0], parts[1]))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static int CountDataRows(string path)
    {
        try
        {
            return File.ReadLines(path).Skip(1).Count(l => !string.IsNullOrWhiteSpace(l));
        }
        catch (IOException)
        {
            return 0;
        }
    }
}
