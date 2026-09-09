using PowerPosition.Core.Domain;
using PowerPosition.Core.Reporting;

namespace PowerPosition.Tests.TestSupport;

public sealed class FakePositionReportWriter(IPositionReportWriter inner) : IPositionReportWriter
{
    private int _callCount;

    public int FailFirstNCalls { get; init; }

    public int CallCount => _callCount;

    public Task<string> WriteAsync(
        IReadOnlyList<PowerPositionRow> rows,
        string outputFolder,
        DateTimeOffset extractedAtLocal,
        CancellationToken cancellationToken)
    {
        _callCount++;
        if (_callCount <= FailFirstNCalls)
        {
            throw new IOException("simulated transient write failure");
        }

        return inner.WriteAsync(rows, outputFolder, extractedAtLocal, cancellationToken);
    }
}
