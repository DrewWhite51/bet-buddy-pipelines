namespace SportsBettingPipeline.Core.Models.HistoricalGame;

/// <summary>
/// Represents a reference to a game extracted from a week summary page.
/// Contains the game ID and URL needed to scrape the full game details.
/// </summary>
public class GameReference
{
    /// <summary>Unique game ID extracted from boxscore URL (e.g., "202409050kan")</summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>Season year</summary>
    public int Year { get; set; }

    /// <summary>Week number within the season</summary>
    public int Week { get; set; }

    /// <summary>Date of the game</summary>
    public DateOnly GameDate { get; set; }

    /// <summary>Home team abbreviation extracted from game ID (e.g., "kan")</summary>
    public string HomeTeamCode { get; set; } = string.Empty;

    /// <summary>Relative boxscore URL (e.g., "/boxscores/202409050kan.htm")</summary>
    public string BoxscoreUrl { get; set; } = string.Empty;

    /// <summary>Full URL for scraping the game details</summary>
    public string FullUrl => $"https://www.pro-football-reference.com{BoxscoreUrl}";

    /// <summary>CSV header row</summary>
    public static string CsvHeader => "GameId,Year,Week,GameDate,HomeTeamCode,BoxscoreUrl";

    /// <summary>Converts this game reference to a CSV line</summary>
    public string ToCsvLine() => $"{GameId},{Year},{Week},{GameDate:yyyy-MM-dd},{HomeTeamCode},{BoxscoreUrl}";

    /// <summary>Parses a game ID into its components</summary>
    /// <param name="gameId">Game ID like "202409050kan"</param>
    /// <returns>Tuple of (year, month, day, teamCode)</returns>
    public static (int Year, int Month, int Day, string TeamCode) ParseGameId(string gameId)
    {
        if (string.IsNullOrEmpty(gameId) || gameId.Length < 12)
            throw new ArgumentException($"Invalid game ID format: {gameId}", nameof(gameId));

        var year = int.Parse(gameId[..4]);
        var month = int.Parse(gameId[4..6]);
        var day = int.Parse(gameId[6..8]);
        var teamCode = gameId[9..];

        return (year, month, day, teamCode);
    }

    /// <summary>Extracts game ID from a boxscore URL</summary>
    /// <param name="boxscoreUrl">URL like "/boxscores/202409050kan.htm"</param>
    /// <returns>Game ID like "202409050kan"</returns>
    public static string ExtractGameIdFromUrl(string boxscoreUrl)
    {
        return Path.GetFileNameWithoutExtension(boxscoreUrl);
    }

    /// <summary>Creates a GameReference from a boxscore URL</summary>
    public static GameReference FromBoxscoreUrl(string boxscoreUrl, int year, int week)
    {
        var gameId = ExtractGameIdFromUrl(boxscoreUrl);
        var (_, month, day, teamCode) = ParseGameId(gameId);

        return new GameReference
        {
            GameId = gameId,
            Year = year,
            Week = week,
            GameDate = new DateOnly(year, month, day),
            HomeTeamCode = teamCode,
            BoxscoreUrl = boxscoreUrl
        };
    }

    public override string ToString() => $"{GameId} (Week {Week}, {GameDate:yyyy-MM-dd})";
}
