using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using SportsBettingPipeline.Core.Models;
using SportsBettingPipeline.Infrastructure.Data;
using SportsBettingPipeline.Scrapers;

// --- Parse Command Line Arguments ---
var dryRun = args.Contains("--dry-run") || args.Contains("-d");
var showHelp = args.Contains("--help") || args.Contains("-h");
var runPff = args.Contains("--pff");
var runPffExtract = args.Contains("--pff-extract");
var skipMove = args.Contains("--skip-move");

if (showHelp)
{
    Console.WriteLine(@"
Sports Betting Pipeline - Scraper CLI

Usage: dotnet run --project Sandbox [options]

Scraper Selection:
  --pff               Run PFF week-by-week HTML scraper (default: historical lines)
  --pff-extract       Extract game references from week HTML to CSV

Options:
  --dry-run, -d       Scrape and parse data but don't upload to S3 (preview only)
  --year, -y <year>   Scrape a single year (e.g., --year 2024)
  --from <year>       Start year for range (default: 1952 for lines, 2000 for PFF)
  --to <year>         End year for range (default: 2025)
  --week <N>          Process specific week only (for --pff-extract)
  --skip-move         Don't move week files to processed after extraction
  --help, -h          Show this help message

Examples:
  dotnet run --project Sandbox --dry-run --year 2024
  dotnet run --project Sandbox --from 2020 --to 2025
  dotnet run --project Sandbox --pff --year 2024
  dotnet run --project Sandbox --pff --from 2020 --to 2024 --dry-run
  dotnet run --project Sandbox --pff-extract --year 2024 --dry-run
  dotnet run --project Sandbox --pff-extract --year 2024 --week 1
  dotnet run --project Sandbox -d -y 2023
  dotnet run --project Sandbox                         # Full range 1952-2025 to S3
");
    return;
}

// Parse year and week arguments
int? singleYear = null;
int? singleWeek = null;
int fromYear = 1952;
int toYear = 2025;

for (int i = 0; i < args.Length; i++)
{
    if ((args[i] == "--year" || args[i] == "-y") && i + 1 < args.Length)
    {
        if (int.TryParse(args[i + 1], out var y))
            singleYear = y;
    }
    else if (args[i] == "--from" && i + 1 < args.Length)
    {
        if (int.TryParse(args[i + 1], out var y))
            fromYear = y;
    }
    else if (args[i] == "--to" && i + 1 < args.Length)
    {
        if (int.TryParse(args[i + 1], out var y))
            toYear = y;
    }
    else if (args[i] == "--week" && i + 1 < args.Length)
    {
        if (int.TryParse(args[i + 1], out var w))
            singleWeek = w;
    }
}

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var settings = new AppSettings();
configuration.Bind(settings);

// Allow flat environment variable overrides
var s3BucketOverride = Environment.GetEnvironmentVariable("S3_BUCKET_NAME");
if (!string.IsNullOrEmpty(s3BucketOverride))
    settings.Aws.S3BucketName = s3BucketOverride;

var awsRegionOverride = Environment.GetEnvironmentVariable("AWS_REGION");
if (!string.IsNullOrEmpty(awsRegionOverride))
    settings.Aws.Region = awsRegionOverride;

var dbConnectionOverride = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
if (!string.IsNullOrEmpty(dbConnectionOverride))
    settings.Database.ConnectionString = dbConnectionOverride;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("=== Sports Betting Pipeline Sandbox ===");
Log.Information("Config - S3 Bucket: {Bucket}, Region: {Region}", settings.Aws.S3BucketName, settings.Aws.Region);
Log.Information("Config - Database: {Status}", !string.IsNullOrEmpty(settings.Database.ConnectionString) ? "configured" : "not configured");

if (dryRun)
    Log.Information("Mode: DRY RUN (no S3 uploads)");

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

var historicalScraper = new HistoricalLinesScraper(httpClient, Log.Logger);
var pffScraper = new PFFScraper(httpClient, Log.Logger);

// --- Determine years to scrape ---
// Adjust default start year for PFF scrapers
if ((runPff || runPffExtract) && fromYear == 1952)
{
    fromYear = 2000;
}

int[] yearsToScrape;
if (singleYear.HasValue)
{
    yearsToScrape = new[] { singleYear.Value };
    Log.Information("Scraping single year: {Year}", singleYear.Value);
}
else
{
    yearsToScrape = Enumerable.Range(fromYear, toYear - fromYear + 1).Reverse().ToArray();
    Log.Information("Scraping year range: {From} to {To} ({Count} seasons)", fromYear, toYear, yearsToScrape.Length);
}

// --- Execute Selected Scraper ---
if (runPffExtract)
{
    // --- PFF Game Reference Extraction ---
    Log.Information("=== PFF Game Reference Extraction ===");

    if (!singleYear.HasValue)
    {
        Log.Error("--pff-extract requires --year <year> to be specified");
        return;
    }

    var year = singleYear.Value;

    if (dryRun)
    {
        Log.Information("--- DRY RUN: Extracting game references (no S3 upload) ---");

        if (!string.IsNullOrEmpty(settings.Aws.S3BucketName))
        {
            try
            {
                var s3Client = new Amazon.S3.AmazonS3Client(
                    Amazon.RegionEndpoint.GetBySystemName(settings.Aws.Region));

                if (singleWeek.HasValue)
                {
                    var (gameCount, games) = await pffScraper.ExtractWeekDryRunAsync(
                        s3Client, settings.Aws.S3BucketName, year, singleWeek.Value);

                    Log.Information("  Week {Week}: {Count} games", singleWeek.Value, gameCount);
                    foreach (var game in games.Take(5))
                    {
                        Log.Information("    {GameId} - {Date} - {Url}", game.GameId, game.GameDate, game.BoxscoreUrl);
                    }
                    if (games.Count > 5)
                        Log.Information("    ... and {More} more games", games.Count - 5);
                }
                else
                {
                    var results = await pffScraper.ExtractYearDryRunAsync(
                        s3Client, settings.Aws.S3BucketName, year);

                    foreach (var (week, gameCount) in results)
                    {
                        Log.Information("  Week {Week}: {Count} games", week, gameCount);
                    }

                    var totalGames = results.Sum(r => r.GameCount);
                    Log.Information("  Total: {TotalGames} games from {WeekCount} weeks", totalGames, results.Count);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Extraction dry run failed");
            }
        }
        else
        {
            Log.Warning("No S3 bucket configured. --pff-extract requires S3 bucket to read week HTML.");
        }
    }
    else if (!string.IsNullOrEmpty(settings.Aws.S3BucketName))
    {
        Log.Information("--- Extracting game references to S3 ---");

        try
        {
            var s3Client = new Amazon.S3.AmazonS3Client(
                Amazon.RegionEndpoint.GetBySystemName(settings.Aws.Region));

            if (singleWeek.HasValue)
            {
                var result = await pffScraper.ExtractWeekToS3Async(
                    s3Client, settings.Aws.S3BucketName, year, singleWeek.Value, skipMove);

                if (result.Success)
                {
                    Log.Information("  Week {Week}: {Count} games -> {Key}",
                        singleWeek.Value, result.GameCount, result.S3Key);
                }
                else
                {
                    Log.Error("  Week {Week}: {Error}", singleWeek.Value, result.Error);
                }
            }
            else
            {
                var yearResult = await pffScraper.ExtractYearToS3Async(
                    s3Client, settings.Aws.S3BucketName, year, skipMove);

                Log.Information("--- Extraction Results ---");
                foreach (var weekResult in yearResult.WeekResults)
                {
                    if (weekResult.Success)
                    {
                        Log.Information("  Week {Week}: {Count} games -> {Key}",
                            weekResult.Week, weekResult.GameCount, weekResult.S3Key);
                    }
                    else
                    {
                        Log.Warning("  Week {Week}: {Error}", weekResult.Week, weekResult.Error);
                    }
                }

                Log.Information("  Total: {TotalGames} games from {SuccessWeeks}/{TotalWeeks} weeks",
                    yearResult.TotalGames, yearResult.SuccessfulWeeks, yearResult.WeekResults.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Extraction failed");
        }
    }
    else
    {
        Log.Warning("No S3 bucket configured. Set S3_BUCKET_NAME environment variable or configure in appsettings.json");
    }
}
else if (runPff)
{
    // --- PFF Week-by-Week HTML Scraping ---
    Log.Information("=== PFF Week-by-Week HTML Scraper ===");

    if (dryRun)
    {
        Log.Information("--- DRY RUN: PFF Scraping (no S3 upload) ---");

        foreach (var year in yearsToScrape)
        {
            try
            {
                var weekResults = await pffScraper.ScrapeYearDryRunAsync(year);
                Log.Information("  {Year}: {Count} weeks found", year, weekResults.Count);

                foreach (var (week, length, key) in weekResults.Take(3))
                {
                    Log.Information("    Week {Week}: {Length} bytes -> {Key}", week, length, key);
                }
                if (weekResults.Count > 3)
                    Log.Information("    ... and {More} more weeks", weekResults.Count - 3);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to dry-run year {Year}", year);
            }
        }
    }
    else if (!string.IsNullOrEmpty(settings.Aws.S3BucketName))
    {
        Log.Information("--- PFF Scraping to S3 ---");

        try
        {
            var s3Client = new Amazon.S3.AmazonS3Client(
                Amazon.RegionEndpoint.GetBySystemName(settings.Aws.Region));

            var results = await pffScraper.ScrapeMultipleYearsToS3Async(
                s3Client, settings.Aws.S3BucketName, yearsToScrape);

            Log.Information("--- PFF Upload Results ---");
            foreach (var (year, weekResults) in results)
            {
                var successCount = weekResults.Count(kv => !kv.Value.StartsWith("ERROR"));
                Log.Information("  {Year}: {Success}/{Total} weeks uploaded",
                    year, successCount, weekResults.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PFF S3 upload failed");
        }
    }
    else
    {
        Log.Warning("No S3 bucket configured. PFF scraper requires S3 for output.");
        Log.Information("Set S3_BUCKET_NAME environment variable or configure in appsettings.json");
    }
}
else
{
    // --- Historical Odds Scraping ---
    if (dryRun)
    {
        // Dry run: scrape and preview, don't upload
        Log.Information("--- DRY RUN: Scraping Historical Odds (no S3 upload) ---");

        foreach (var year in yearsToScrape)
        {
            try
            {
                var games = await historicalScraper.ParseHistoricalTablesAsync(year);
                Log.Information("  {Year}: {Count} games parsed", year, games.Count);

                // Show first 3 games as preview
                foreach (var game in games.Take(3))
                {
                    Log.Information("    {Game}", game.ToString());
                }
                if (games.Count > 3)
                    Log.Information("    ... and {More} more", games.Count - 3);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to scrape year {Year}", year);
            }
        }
    }
    else if (!string.IsNullOrEmpty(settings.Aws.S3BucketName))
    {
        // Upload to S3
        Log.Information("--- Scraping Historical Odds to S3 ---");

        try
        {
            var s3Client = new Amazon.S3.AmazonS3Client(
                Amazon.RegionEndpoint.GetBySystemName(settings.Aws.Region));

            var results = await historicalScraper.ScrapeMultipleYearsToS3Async(
                s3Client, settings.Aws.S3BucketName, yearsToScrape);

            Log.Information("--- Upload Results ---");
            foreach (var (year, key) in results)
            {
                Log.Information("  {Year}: {Key}", year, key);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Historical odds S3 upload failed");
        }
    }
    else
    {
        // No S3 configured - save to local files
        Log.Information("--- Scraping Historical Odds to Local Files (no S3 bucket configured) ---");

        foreach (var year in yearsToScrape)
        {
            try
            {
                var filePath = await historicalScraper.SaveCsvToFileAsync(year);
                Log.Information("  {Year}: {Path}", year, filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to scrape year {Year}", year);
            }
        }
    }
}

Log.Information("Sandbox finished.");
