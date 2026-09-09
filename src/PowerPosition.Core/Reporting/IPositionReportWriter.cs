using PowerPosition.Core.Domain;

namespace PowerPosition.Core.Reporting;

public interface IPositionReportWriter
{
    Task<string> WriteAsync(
        IReadOnlyList<PowerPositionRow> rows,
        string outputFolder,
        DateTimeOffset extractedAtLocal,
        CancellationToken cancellationToken);
}
