using Amazon.S3;
using Amazon.S3.Model;
using AngleSharp.Html.Dom;
using Serilog;
using SportsBettingPipeline.Core.Models;
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
}
