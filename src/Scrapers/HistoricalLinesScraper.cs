using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Serilog;
using SportsBettingPipeline.Core.Models;
using SportsBettingPipeline.Core.Models.HistoricalGame;
using SportsBettingPipeline.Core.Scrapers;


namespace SportsBettingPipeline.Scrapers;


public class HistoricalLinesScraper : BaseScraperService
{
    private const string BaseUrl = "https://www.covers.com/sportsoddshistory/nfl-game-season/?y=";
    private const string SportsbookName = "HistoricalLines";

    public HistoricalLinesScraper(HttpClient httpClient, ILogger logger)
        : base(httpClient, logger, rateLimitDelayMs: 1500)
    {
    }

    protected override OddsData ParseHtml(IHtmlDocument document)
    {
        // TODO: Implement parsing logic for HistoricalLines
        return new OddsData();
    }
    
    public string HelloHistoricalScraper()
    {
        return "Hello from HistoricalLinesScraper!";
    }

    public async Task<OddsData> ScrapeHistoricalOddsAsync(int year = 2025)
    {
        return await ScrapeOddsAsync($"{BaseUrl}{year}");
    }

    /// <summary>
    /// Fetches the HTML from the historical odds URL and logs it to the console.
    /// Useful for debugging and developing the ParseHtml implementation.
    /// </summary>
    public async Task<string> DumpHtmlAsync(int year = 2025)
    {
        var url = $"{BaseUrl}{year}";
        Logger.Information("Fetching HTML from {Url}", url);

        var html = await HttpClient.GetStringAsync(url);

        Logger.Information("=== RAW HTML START ({Length} chars) ===", html.Length);
        Console.WriteLine(html);
        Logger.Information("=== RAW HTML END ===");

        return html;
    }

    /// <summary>
    /// Fetches the HTML and saves it to a file, then returns the path.
    /// </summary>
    public async Task<string> DumpHtmlToFileAsync(int year = 2025, string? outputPath = null)
    {
        var url = $"{BaseUrl}{year}";
        var html = await HttpClient.GetStringAsync(url);

        outputPath ??= Path.Combine(Path.GetTempPath(), $"historical-odds-{year}.html");
        await File.WriteAllTextAsync(outputPath, html);

        Logger.Information("HTML written to {Path} ({Length} chars)", outputPath, html.Length);

        return outputPath;
    }

    /// <summary>
    /// Parses all historical game tables from the page for a specific year.
    /// Each table represents games from a specific week/round.
    /// </summary>
    public async Task<List<HistoricalGameRow>> ParseHistoricalTablesAsync(int year = 2025)
    {
        var url = $"{BaseUrl}{year}";
        Logger.Information("Fetching and parsing historical odds for {Year} from {Url}", year, url);

        await RateLimitDelay();

        var html = await HttpClient.GetStringAsync(url);

        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html);

        var games = new List<HistoricalGameRow>();

        // Find all tbody elements - each table body contains game rows
        var tableBodies = document.QuerySelectorAll("tbody");

        Logger.Information("Found {Count} table bodies to parse for {Year}", tableBodies.Length, year);

        // Valid day abbreviations for game rows
        var validDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"
        };

        foreach (var tbody in tableBodies)
        {
            var rows = tbody.QuerySelectorAll("tr");

            foreach (var row in rows)
            {
                var cells = row.QuerySelectorAll("td");

                // Require at least 5 cells (day, date, time, and some team data)
                // Older seasons may have fewer columns (no over/under, etc.)
                if (cells.Length < 5)
                    continue;

                // Detect table format:
                // - Regular season: Day is at index 0
                // - Playoffs: Round is at index 0, Day is at index 1
                var cell0 = GetCellText(cells, 0);
                var cell1 = GetCellText(cells, 1);

                int offset;
                string dayCell;
                string notes;

                if (validDays.Contains(cell0))
                {
                    // Regular season format: Day at index 0
                    offset = 0;
                    dayCell = cell0;
                    notes = SafeGetCellText(cells, 10);
                }
                else if (validDays.Contains(cell1))
                {
                    // Playoff format: Round at index 0, Day at index 1
                    offset = 1;
                    dayCell = cell1;
                    notes = cell0; // Use the Round column as notes (e.g., "AFC Wild Card")
                }
                else
                {
                    // Neither format matches - skip this row (probably stats or headers)
                    continue;
                }

                // Safely extract data - column indices shifted by offset for playoff games
                var game = new HistoricalGameRow
                {
                    Year = year,
                    Day = dayCell,
                    Date = SafeGetCellText(cells, 1 + offset),
                    Time = SafeGetCellText(cells, 2 + offset),
                    FavoriteLocation = SafeGetCellText(cells, 3 + offset),
                    Favorite = SafeGetCellText(cells, 4 + offset),
                    Score = SafeGetCellText(cells, 5 + offset),
                    SpreadResult = SafeGetCellText(cells, 6 + offset),
                    UnderdogLocation = SafeGetCellText(cells, 7 + offset),
                    Underdog = SafeGetCellText(cells, 8 + offset),
                    OverUnderResult = SafeGetCellText(cells, 9 + offset),
                    Notes = notes,
                    FavoriteCovered = SafeIsCellBold(cells, 4 + offset),
                    UnderdogCovered = SafeIsCellBold(cells, 8 + offset)
                };

                games.Add(game);
            }
        }

        Logger.Information("Parsed {Count} historical games for {Year}", games.Count, year);

        return games;
    }

    /// <summary>
    /// Converts a list of games to CSV format.
    /// </summary>
    public string ToCsv(List<HistoricalGameRow> games)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Year,Day,Date,Time,FavoriteLocation,Favorite,Score,SpreadResult,UnderdogLocation,Underdog,OverUnderResult,Notes,FavoriteCovered,UnderdogCovered,CoveredBy");

        foreach (var game in games)
        {
            sb.AppendLine($"{game.Year},{EscapeCsv(game.Day)},{EscapeCsv(game.Date)},{EscapeCsv(game.Time)},{EscapeCsv(game.FavoriteLocation)},{EscapeCsv(game.Favorite)},{EscapeCsv(game.Score)},{EscapeCsv(game.SpreadResult)},{EscapeCsv(game.UnderdogLocation)},{EscapeCsv(game.Underdog)},{EscapeCsv(game.OverUnderResult)},{EscapeCsv(game.Notes)},{game.FavoriteCovered},{game.UnderdogCovered},{game.CoveredBy}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a string for CSV format (handles commas and quotes).
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Scrapes historical odds for a year and uploads to S3 as CSV.
    /// Idempotent: same year always produces same S3 key, overwriting previous data.
    /// Returns the S3 key of the uploaded file.
    /// </summary>
    public async Task<string> ScrapeAndUploadToS3Async(IAmazonS3 s3Client, string bucketName, int year)
    {
        var games = await ParseHistoricalTablesAsync(year);
        var csv = ToCsv(games);

        var s3Key = $"historical-lines-data/{year}_nfl_odds.csv";

        Logger.Information("Uploading {Count} games for {Year} to s3://{Bucket}/{Key}", games.Count, year, bucketName, s3Key);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = s3Key,
            ContentBody = csv,
            ContentType = "text/csv"
        };

        await s3Client.PutObjectAsync(request);

        Logger.Information("Successfully uploaded {Year} historical odds to S3: {Key}", year, s3Key);

        return s3Key;
    }

    /// <summary>
    /// Scrapes historical odds for multiple years and uploads each to S3.
    /// Returns a dictionary of year to S3 key.
    /// </summary>
    public async Task<Dictionary<int, string>> ScrapeMultipleYearsToS3Async(IAmazonS3 s3Client, string bucketName, params int[] years)
    {
        var results = new Dictionary<int, string>();

        foreach (var year in years)
        {
            try
            {
                var s3Key = await ScrapeAndUploadToS3Async(s3Client, bucketName, year);
                results[year] = s3Key;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to scrape/upload year {Year}", year);
                results[year] = $"ERROR: {ex.Message}";
            }
        }

        return results;
    }

    /// <summary>
    /// Saves CSV to a local file. Useful for testing without S3.
    /// </summary>
    public async Task<string> SaveCsvToFileAsync(int year, string? outputPath = null)
    {
        var games = await ParseHistoricalTablesAsync(year);
        var csv = ToCsv(games);

        outputPath ??= Path.Combine(Path.GetTempPath(), $"{year}_nfl_odds.csv");
        await File.WriteAllTextAsync(outputPath, csv);

        Logger.Information("CSV written to {Path} ({Count} games)", outputPath, games.Count);

        return outputPath;
    }

    /// <summary>
    /// Safely extracts text content from a cell at the given index.
    /// Returns empty string if index is out of bounds.
    /// </summary>
    private static string GetCellText(AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> cells, int index)
    {
        if (index >= cells.Length)
            return string.Empty;

        // Get the text content, trimming whitespace
        return cells[index].TextContent?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Safely extracts text content, handling missing/blank columns gracefully.
    /// Used for historical data where older seasons may have fewer columns.
    /// </summary>
    private static string SafeGetCellText(AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> cells, int index)
    {
        if (index < 0 || index >= cells.Length)
            return string.Empty;

        var text = cells[index].TextContent?.Trim() ?? string.Empty;

        // Return empty for cells that only contain whitespace or non-breaking spaces
        if (string.IsNullOrWhiteSpace(text) || text == "\u00A0")
            return string.Empty;

        return text;
    }

    /// <summary>
    /// Checks if the cell content is bold (team covered the spread).
    /// Bold can be indicated by &lt;b&gt;, &lt;strong&gt;, or font-weight CSS.
    /// </summary>
    private static bool IsCellBold(AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> cells, int index)
    {
        if (index >= cells.Length)
            return false;

        var cell = cells[index];

        // Check for <b> or <strong> tags
        if (cell.QuerySelector("b") != null || cell.QuerySelector("strong") != null)
            return true;

        // Check if the cell itself has a bold-related class or style
        var style = cell.GetAttribute("style") ?? "";
        if (style.Contains("font-weight") && (style.Contains("bold") || style.Contains("700")))
            return true;

        var className = cell.ClassName ?? "";
        if (className.Contains("bold", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Safely checks if a cell is bold, returning false for out-of-bounds indices.
    /// </summary>
    private static bool SafeIsCellBold(AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> cells, int index)
    {
        if (index < 0 || index >= cells.Length)
            return false;

        return IsCellBold(cells, index);
    }
}