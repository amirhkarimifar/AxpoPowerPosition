using System.Globalization;
using System.Text.RegularExpressions;

namespace PowerPosition.Dashboard.Services;

public sealed record LogEntry(DateTimeOffset Timestamp, string Level, string Message);

public sealed partial class LogFileService(MonitorPaths paths)
{
    [GeneratedRegex(@"^(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[(?<level>\w{3})\] (?<message>.*)$")]
    private static partial Regex EntryStart();

    public string? LatestLogFileName { get; private set; }

    public IReadOnlyList<LogEntry> ReadRecent(int maxEntries = 300)
    {
        if (!Directory.Exists(paths.LogsFolder))
        {
            LatestLogFileName = null;
            return [];
        }

        var latestFile = new DirectoryInfo(paths.LogsFolder)
            .GetFiles("*.log")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        LatestLogFileName = latestFile?.Name;
        if (latestFile is null)
        {
            return [];
        }

        List<LogEntry> entries = [];
        LogEntry? current = null;

        using var stream = new FileStream(latestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var match = EntryStart().Match(line);
            if (match.Success)
            {
                if (current is not null)
                {
                    entries.Add(current);
                }

                var timestamp = DateTimeOffset.Parse(match.Groups["ts"].Value, CultureInfo.InvariantCulture);
                current = new LogEntry(timestamp, match.Groups["level"].Value, match.Groups["message"].Value);
            }
            else if (current is not null && !string.IsNullOrWhiteSpace(line))
            {
                current = current with { Message = current.Message + "\n" + line.Trim() };
            }
        }

        if (current is not null)
        {
            entries.Add(current);
        }

        return entries.Count <= maxEntries ? entries : entries.GetRange(entries.Count - maxEntries, maxEntries);
    }
}
