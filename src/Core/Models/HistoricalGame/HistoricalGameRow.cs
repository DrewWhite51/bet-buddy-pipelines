namespace SportsBettingPipeline.Core.Models.HistoricalGame;

/// <summary>
/// Represents a single row from a historical odds table.
/// Each row contains game information including teams, scores, spreads, and over/under results.
/// </summary>
public class HistoricalGameRow
{
    /// <summary>Season year (e.g., 2024, 2025)</summary>
    public int Year { get; set; }

    /// <summary>Day of the week (e.g., "Sun", "Mon", "Thu")</summary>
    public string Day { get; set; } = string.Empty;

    /// <summary>Date of the game (e.g., "Jan 05")</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Game time (e.g., "1:00 PM")</summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>Location indicator for favorite ("@" = away, "N" = neutral, empty = home)</summary>
    public string FavoriteLocation { get; set; } = string.Empty;

    /// <summary>Favorite team name</summary>
    public string Favorite { get; set; } = string.Empty;

    /// <summary>Final score (e.g., "27-24")</summary>
    public string Score { get; set; } = string.Empty;

    /// <summary>Spread result (e.g., "W -3", "L +7")</summary>
    public string SpreadResult { get; set; } = string.Empty;

    /// <summary>Location indicator for underdog ("@" = away, "N" = neutral, empty = home)</summary>
    public string UnderdogLocation { get; set; } = string.Empty;

    /// <summary>Underdog team name</summary>
    public string Underdog { get; set; } = string.Empty;

    /// <summary>Over/Under result (e.g., "O 45.5", "U 51.5")</summary>
    public string OverUnderResult { get; set; } = string.Empty;

    /// <summary>Additional notes (e.g., "OT", playoff round info)</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>True if the favorite covered the spread (indicated by bold in source)</summary>
    public bool FavoriteCovered { get; set; }

    /// <summary>True if the underdog covered the spread (indicated by bold in source)</summary>
    public bool UnderdogCovered { get; set; }

    /// <summary>Which team covered: "Favorite", "Underdog", "Push", or "Unknown"</summary>
    public string CoveredBy => FavoriteCovered ? "Favorite" : UnderdogCovered ? "Underdog" : "Unknown";

    public override string ToString()
    {
        var favMarker = FavoriteCovered ? "*" : "";
        var dogMarker = UnderdogCovered ? "*" : "";
        return $"{Day} {Date} {Time} | {favMarker}{FavoriteLocation}{Favorite}{favMarker} vs {dogMarker}{UnderdogLocation}{Underdog}{dogMarker} | Score: {Score} | Spread: {SpreadResult} | O/U: {OverUnderResult} | Covered: {CoveredBy} | {Notes}";
    }
}
