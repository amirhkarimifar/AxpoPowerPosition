using System.ComponentModel.DataAnnotations;
using PowerPosition.Core.Domain;

namespace PowerPosition.Core.Options;

public sealed class ExtractOptions
{
    public const string SectionName = "Extract";

    [Required]
    public string OutputFolder { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int IntervalMinutes { get; set; } = 60;

    public string TimeZoneId { get; set; } = TimeZoneResolver.DefaultTimeZoneId;

    /// <summary>
    /// Days added to today's date to get the trading date requested from PowerService.
    /// 1 (default) is the day-ahead position — the standard day-ahead-market reading of
    /// "day ahead power position": today's report covers tomorrow's delivery day. 0 requests
    /// the same day instead.
    /// </summary>
    public int DayOffset { get; set; } = 1;
}
