using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using SportsBettingPipeline.Core.Models;
using SportsBettingPipeline.Infrastructure.Data;
using SportsBettingPipeline.Scrapers;

// --- Parse Command Line Arguments ---
var dryRun = args.Contains("--dry-run") || args.Contains("-d");
var showHelp = args.Contains("--help") || args.Contains("-h");

if (showHelp)
{
    Console.WriteLine(@"
Sports Betting Pipeline - Historical Odds Scraper

Usage: dotnet run --project Sandbox [options]

Options:
  --dry-run, -d       Scrape and parse data but don't upload to S3 (preview only)
  --year, -y <year>   Scrape a single year (e.g., --year 2024)
  --from <year>       Start year for range (default: 1952)
  --to <year>         End year for range (default: 2025)
  --help, -h          Show this help message

Examples:
  dotnet run --project Sandbox --dry-run --year 2024
  dotnet run --project Sandbox --from 2020 --to 2025
  dotnet run --project Sandbox -d -y 2023
  dotnet run --project Sandbox                         # Full range 1952-2025 to S3
");
    return;
}

// Parse year arguments
int? singleYear = null;
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

// --- Determine years to scrape ---
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


// try
// {
//     var odds = await scraper.ScrapeOddsAsync(url);

//     Log.Information("Scrape succeeded!");
//     Log.Information("  Sportsbook: {Sportsbook}", odds.Sportsbook);
//     Log.Information("  Sport:      {Sport}", odds.Sport);
//     Log.Information("  Team1:      {Team1}", odds.Team1);
//     Log.Information("  Team2:      {Team2}", odds.Team2);
//     Log.Information("  Spread:     {Spread}", odds.Spread?.ToString() ?? "N/A");
//     Log.Information("  Moneyline:  {Moneyline}", odds.Moneyline?.ToString() ?? "N/A");
//     Log.Information("  OverUnder:  {OverUnder}", odds.OverUnder?.ToString() ?? "N/A");
//     Log.Information("  Timestamp:  {Timestamp}", odds.Timestamp);
// }
// catch (Exception ex)
// {
//     Log.Error(ex, "Scraper failed");
//     Log.Information("This is expected if the page requires JavaScript rendering or the CSS selectors need updating");
// }

// --- S3 Write Example ---
// Writes a sample OddsData object to S3 as JSON.
// Requires: a valid S3 bucket name in appsettings.json and AWS credentials configured
//   (via aws configure, env vars AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY, or IAM role)
// if (!string.IsNullOrEmpty(settings.Aws.S3BucketName))
// {
//     Log.Information("--- S3 Write Test ---");
//     var s3Sample = new OddsData
//     {
//         Sportsbook = "DraftKings",
//         Sport = "NFL",
//         Team1 = "Kansas City Chiefs",
//         Team2 = "Philadelphia Eagles",
//         Spread = -3.5m,
//         Moneyline = -175m,
//         OverUnder = 48.5m,
//         Timestamp = DateTime.UtcNow
//     };

//     try
//     {
//         var s3Client = new Amazon.S3.AmazonS3Client(
//             Amazon.RegionEndpoint.GetBySystemName(settings.Aws.Region));
//         var s3Storage = new SportsBettingPipeline.Infrastructure.S3StorageService(
//             s3Client, settings.Aws.S3BucketName, Log.Logger);

//         var s3Key = await s3Storage.StoreOddsAsync(s3Sample);
//         Log.Information("S3 write succeeded! Key: {Key}", s3Key);
//     }
//     catch (Exception ex)
//     {
//         Log.Error(ex, "S3 write failed");
//         Log.Information("Check that the bucket exists and your AWS credentials are configured");
//     }
// }
// else
// {
//     Log.Information("--- S3 Write Test: SKIPPED (no bucket configured in appsettings.json) ---");
// }

// Example: use database storage (uncomment when PostgreSQL is running)
// var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
//     .UseNpgsql(settings.Database.ConnectionString).Options;
// var db = new AppDbContext(dbOptions);
// var dbStorage = new SportsBettingPipeline.Infrastructure.DbStorageService(db, Log.Logger);

Log.Information("Sandbox finished.");
