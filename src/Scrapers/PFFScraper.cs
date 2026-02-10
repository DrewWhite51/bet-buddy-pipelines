using Amazon.S3;
using Amazon.S3.Model;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Serilog;
using SportsBettingPipeline.Core.Models;
using SportsBettingPipeline.Core.Models.HistoricalGame;
using SportsBettingPipeline.Core.Scrapers;

namespace SportsBettingPipeline.Scrapers;

public class PFFScraper : BaseScraperService
{
    private const string WeekUrlTemplate = "https://www.pro-football-reference.com/years/{0}/week_{1}.htm";
    private const string NotFoundIndicator = "Page Not Found (404 error)";

    public PFFScraper(HttpClient httpClient, ILogger logger)
        : base(httpClient, logger, rateLimitDelayMs: 3000)
    {
    }

    protected override OddsData ParseHtml(IHtmlDocument document)
    {
        // TODO: Implement parsing logic
        return new OddsData();
    }

    public string HelloPFFScraper()
    {
        return "Hello from PFFScraper!";
    }

    public async Task<string> DumpHtmlAsync(string url)
    {
        Logger.Information("Fetching HTML from {Url}", url);
        var html = await HttpClient.GetStringAsync(url);
        Logger.Information("Fetched {Length} chars", html.Length);
        Console.WriteLine(html);
        return html;
    }

    public async Task<string> DumpHtmlToFileAsync(string url, string? outputPath = null)
    {
        var html = await HttpClient.GetStringAsync(url);
        outputPath ??= Path.Combine(Path.GetTempPath(), "pff-dump.html");
        await File.WriteAllTextAsync(outputPath, html);
        Logger.Information("HTML written to {Path} ({Length} chars)", outputPath, html.Length);
        return outputPath;
    }

    /// <summary>
    /// Fetches HTML for a specific year/week. Returns null if 404 is detected.
    /// </summary>
    private async Task<string?> FetchWeekHtmlAsync(int year, int week)
    {
        var url = string.Format(WeekUrlTemplate, year, week);
        Logger.Debug("Fetching {Url}", url);

        await RateLimitDelay();

        try
        {
            var html = await HttpClient.GetStringAsync(url);

            // Check for soft 404 (page returns 200 but contains 404 message)
            if (html.Contains(NotFoundIndicator))
            {
                Logger.Information("Detected 404 for year {Year} week {Week}", year, week);
                return null;
            }

            return html;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Logger.Information("HTTP 404 for year {Year} week {Week}", year, week);
            return null;
        }
    }

    /// <summary>
    /// Generates S3 key in format: pff-historical-games/unprocessed/{year}/week{N}.html
    /// </summary>
    private static string GenerateS3Key(int year, int week)
    {
        return $"pff-historical-games/unprocessed/{year}/week{week}.html";
    }

    /// <summary>
    /// Scrapes all available weeks for a year and uploads raw HTML to S3.
    /// Iterates weeks 1, 2, 3... until a 404 is encountered.
    /// </summary>
    public async Task<Dictionary<int, string>> ScrapeYearToS3Async(IAmazonS3 s3Client, string bucketName, int year)
    {
        var results = new Dictionary<int, string>();
        var week = 1;

        Logger.Information("Starting PFF scrape for year {Year}", year);

        while (true)
        {
            var html = await FetchWeekHtmlAsync(year, week);

            if (html == null)
            {
                Logger.Information("Finished year {Year} after {WeekCount} weeks", year, week - 1);
                break;
            }

            try
            {
                var s3Key = GenerateS3Key(year, week);

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = s3Key,
                    ContentBody = html,
                    ContentType = "text/html"
                };

                await s3Client.PutObjectAsync(request);

                Logger.Information("Uploaded year {Year} week {Week} to s3://{Bucket}/{Key} ({Length} bytes)",
                    year, week, bucketName, s3Key, html.Length);

                results[week] = s3Key;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to upload year {Year} week {Week}", year, week);
                results[week] = $"ERROR: {ex.Message}";
            }

            week++;
        }

        return results;
    }

    /// <summary>
    /// Dry run: scrapes all weeks for a year without uploading.
    /// </summary>
    public async Task<List<(int Week, int HtmlLength, string ProposedKey)>> ScrapeYearDryRunAsync(int year)
    {
        var results = new List<(int Week, int HtmlLength, string ProposedKey)>();
        var week = 1;

        Logger.Information("DRY RUN: Starting PFF scrape for year {Year}", year);

        while (true)
        {
            var html = await FetchWeekHtmlAsync(year, week);

            if (html == null)
            {
                Logger.Information("DRY RUN: Finished year {Year} after {WeekCount} weeks", year, week - 1);
                break;
            }

            var proposedKey = GenerateS3Key(year, week);
            results.Add((week, html.Length, proposedKey));

            Logger.Information("DRY RUN: Year {Year} week {Week}: {Length} bytes -> {Key}",
                year, week, html.Length, proposedKey);

            week++;
        }

        return results;
    }

    /// <summary>
    /// Scrapes multiple years and uploads to S3.
    /// Continues on failure for individual years.
    /// </summary>
    public async Task<Dictionary<int, Dictionary<int, string>>> ScrapeMultipleYearsToS3Async(
        IAmazonS3 s3Client, string bucketName, params int[] years)
    {
        var results = new Dictionary<int, Dictionary<int, string>>();

        foreach (var year in years)
        {
            try
            {
                var yearResults = await ScrapeYearToS3Async(s3Client, bucketName, year);
                results[year] = yearResults;
                Logger.Information("Completed year {Year}: {Count} weeks uploaded", year, yearResults.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to scrape year {Year}, continuing to next year", year);
                results[year] = new Dictionary<int, string> { { 0, $"YEAR_ERROR: {ex.Message}" } };
            }
        }

        return results;
    }

    #region Game Reference Extraction

    /// <summary>
    /// Generates S3 key for unprocessed week HTML (current location).
    /// </summary>
    private static string GenerateWeekUnprocessedKey(int year, int week)
        => $"pff-historical-games/unprocessed/{year}/week{week}.html";

    /// <summary>
    /// Generates S3 key for processed week HTML.
    /// </summary>
    private static string GenerateWeekProcessedKey(int year, int week)
        => $"pff-historical-games/weeks/processed/{year}/week{week}.html";

    /// <summary>
    /// Generates S3 key for game reference CSV.
    /// </summary>
    private static string GenerateGameReferenceCsvKey(int year, int week)
        => $"pff-historical-games/game-references/{year}/week{week}.csv";

    /// <summary>
    /// Reads an object from S3. Returns null if not found.
    /// </summary>
    public async Task<string?> ReadS3ObjectAsync(IAmazonS3 s3Client, string bucketName, string key)
    {
        try
        {
            var response = await s3Client.GetObjectAsync(bucketName, key);
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Logger.Debug("S3 object not found: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Moves an S3 object from source to destination (copy + delete).
    /// </summary>
    public async Task MoveS3ObjectAsync(IAmazonS3 s3Client, string bucketName, string sourceKey, string destKey)
    {
        await s3Client.CopyObjectAsync(bucketName, sourceKey, bucketName, destKey);
        await s3Client.DeleteObjectAsync(bucketName, sourceKey);
        Logger.Information("Moved s3://{Bucket}/{Source} -> {Dest}", bucketName, sourceKey, destKey);
    }

    /// <summary>
    /// Parses week HTML to extract game references (boxscore URLs).
    /// </summary>
    public List<GameReference> ParseGameReferencesFromHtml(string html, int year, int week)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        var games = new List<GameReference>();
        var seenGameIds = new HashSet<string>();

        // Find all boxscore links: <a href="/boxscores/202409050kan.htm">
        var links = document.QuerySelectorAll("a[href*='/boxscores/']");

        foreach (var link in links)
        {
            var href = link.GetAttribute("href");
            if (string.IsNullOrEmpty(href) || !href.EndsWith(".htm"))
                continue;

            try
            {
                var gameId = GameReference.ExtractGameIdFromUrl(href);

                // Skip if we've already seen this game (duplicates in HTML)
                if (string.IsNullOrEmpty(gameId) || gameId.Length < 12 || seenGameIds.Contains(gameId))
                    continue;

                seenGameIds.Add(gameId);
                var gameRef = GameReference.FromBoxscoreUrl(href, year, week);
                games.Add(gameRef);
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to parse boxscore URL {Href}: {Error}", href, ex.Message);
            }
        }

        Logger.Debug("Parsed {Count} game references from year {Year} week {Week}", games.Count, year, week);
        return games;
    }

    /// <summary>
    /// Extracts game references from a week's HTML and saves to CSV in S3.
    /// </summary>
    public async Task<WeekExtractionResult> ExtractWeekToS3Async(
        IAmazonS3 s3Client,
        string bucketName,
        int year,
        int week,
        bool skipMove = false)
    {
        Logger.Information("Extracting game references for {Year} week {Week}", year, week);

        // Read week HTML from S3
        var weekKey = GenerateWeekUnprocessedKey(year, week);
        var html = await ReadS3ObjectAsync(s3Client, bucketName, weekKey);

        if (html == null)
        {
            var error = $"Week HTML not found at {weekKey}";
            Logger.Warning(error);
            return new WeekExtractionResult(year, week, 0, null, error);
        }

        // Parse game references
        var games = ParseGameReferencesFromHtml(html, year, week);

        if (games.Count == 0)
        {
            Logger.Warning("No games found in {Year} week {Week}", year, week);
            return new WeekExtractionResult(year, week, 0, null, "No games found in HTML");
        }

        // Generate CSV content
        var csvLines = new List<string> { GameReference.CsvHeader };
        csvLines.AddRange(games.Select(g => g.ToCsvLine()));
        var csvContent = string.Join("\n", csvLines);

        // Upload CSV to S3
        var csvKey = GenerateGameReferenceCsvKey(year, week);
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = csvKey,
            ContentBody = csvContent,
            ContentType = "text/csv"
        };

        await s3Client.PutObjectAsync(request);
        Logger.Information("Uploaded {Count} game references to s3://{Bucket}/{Key}",
            games.Count, bucketName, csvKey);

        // Move week HTML to processed
        if (!skipMove)
        {
            var processedKey = GenerateWeekProcessedKey(year, week);
            await MoveS3ObjectAsync(s3Client, bucketName, weekKey, processedKey);
        }

        return new WeekExtractionResult(year, week, games.Count, csvKey);
    }

    /// <summary>
    /// Dry run: extracts game references without uploading to S3.
    /// </summary>
    public async Task<(int GameCount, List<GameReference> Games)> ExtractWeekDryRunAsync(
        IAmazonS3 s3Client,
        string bucketName,
        int year,
        int week)
    {
        Logger.Information("DRY RUN: Extracting game references for {Year} week {Week}", year, week);

        var weekKey = GenerateWeekUnprocessedKey(year, week);
        var html = await ReadS3ObjectAsync(s3Client, bucketName, weekKey);

        if (html == null)
        {
            Logger.Warning("DRY RUN: Week HTML not found at {Key}", weekKey);
            return (0, new List<GameReference>());
        }

        var games = ParseGameReferencesFromHtml(html, year, week);
        Logger.Information("DRY RUN: Found {Count} games in {Year} week {Week}", games.Count, year, week);

        return (games.Count, games);
    }

    /// <summary>
    /// Extracts game references for all weeks in a year.
    /// </summary>
    public async Task<YearExtractionResult> ExtractYearToS3Async(
        IAmazonS3 s3Client,
        string bucketName,
        int year,
        bool skipMove = false)
    {
        Logger.Information("Extracting game references for year {Year}", year);

        var weekResults = new List<WeekExtractionResult>();
        var week = 1;

        while (true)
        {
            var weekKey = GenerateWeekUnprocessedKey(year, week);
            var exists = await ReadS3ObjectAsync(s3Client, bucketName, weekKey);

            if (exists == null)
            {
                Logger.Information("No more weeks found for year {Year} after week {Week}", year, week - 1);
                break;
            }

            try
            {
                var result = await ExtractWeekToS3Async(s3Client, bucketName, year, week, skipMove);
                weekResults.Add(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to extract year {Year} week {Week}", year, week);
                weekResults.Add(new WeekExtractionResult(year, week, 0, null, ex.Message));
            }

            week++;
        }

        var yearResult = new YearExtractionResult(year, weekResults);
        Logger.Information("Completed year {Year}: {TotalGames} games from {SuccessWeeks} weeks",
            year, yearResult.TotalGames, yearResult.SuccessfulWeeks);

        return yearResult;
    }

    /// <summary>
    /// Dry run: extracts game references for all weeks in a year without uploading.
    /// </summary>
    public async Task<List<(int Week, int GameCount)>> ExtractYearDryRunAsync(
        IAmazonS3 s3Client,
        string bucketName,
        int year)
    {
        Logger.Information("DRY RUN: Extracting game references for year {Year}", year);

        var results = new List<(int Week, int GameCount)>();
        var week = 1;

        while (true)
        {
            var weekKey = GenerateWeekUnprocessedKey(year, week);
            var exists = await ReadS3ObjectAsync(s3Client, bucketName, weekKey);

            if (exists == null)
            {
                Logger.Information("DRY RUN: No more weeks found for year {Year} after week {Week}", year, week - 1);
                break;
            }

            var (gameCount, _) = await ExtractWeekDryRunAsync(s3Client, bucketName, year, week);
            results.Add((week, gameCount));
            week++;
        }

        var totalGames = results.Sum(r => r.GameCount);
        Logger.Information("DRY RUN: Year {Year} total: {TotalGames} games from {WeekCount} weeks",
            year, totalGames, results.Count);

        return results;
    }

    #endregion
}
