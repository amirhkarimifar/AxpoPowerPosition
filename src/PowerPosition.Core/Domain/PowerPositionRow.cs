namespace PowerPosition.Core.Domain;

public sealed record PowerPositionRow(int Period, TimeOnly LocalTime, double Volume);
