namespace PowerPosition.Core.Extraction;

public sealed record ExtractResult(string FilePath, int RowCount, DateOnly TradingDate, TimeSpan Elapsed);
