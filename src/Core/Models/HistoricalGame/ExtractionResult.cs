namespace SportsBettingPipeline.Core.Models.HistoricalGame;

/// <summary>
/// Result of extracting game references from a single week's HTML.
/// </summary>
public record WeekExtractionResult(
    int Year,
    int Week,
    int GameCount,
    string? S3Key,
    string? Error = null)
{
    public bool Success => Error == null && S3Key != null;
}

/// <summary>
/// Result of extracting game references from an entire year.
/// </summary>
public record YearExtractionResult(
    int Year,
    List<WeekExtractionResult> WeekResults)
{
    public int TotalGames => WeekResults.Sum(w => w.GameCount);
    public int SuccessfulWeeks => WeekResults.Count(w => w.Success);
    public int FailedWeeks => WeekResults.Count(w => !w.Success);
}
