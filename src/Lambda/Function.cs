using Amazon.Lambda.Core;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using SportsBettingPipeline.Core.Models;
using SportsBettingPipeline.Infrastructure;
using SportsBettingPipeline.Infrastructure.Data;
using SportsBettingPipeline.Scrapers;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SportsBettingPipeline.Lambda;

public class Function
{
    private readonly DraftKingsScraper _scraper;
    private readonly S3StorageService _s3Storage;
    private readonly DbStorageService? _dbStorage;

    public Function()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        // Allow flat environment variable overrides
        var bucketOverride = Environment.GetEnvironmentVariable("S3_BUCKET_NAME");
        if (!string.IsNullOrEmpty(bucketOverride))
            settings.Aws.S3BucketName = bucketOverride;

        var awsRegionOverride = Environment.GetEnvironmentVariable("AWS_REGION");
        if (!string.IsNullOrEmpty(awsRegionOverride))
            settings.Aws.Region = awsRegionOverride;

        var dbConnectionOverride = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(dbConnectionOverride))
            settings.Database.ConnectionString = dbConnectionOverride;

        if (string.IsNullOrEmpty(settings.Aws.S3BucketName))
            throw new InvalidOperationException(
                "S3 bucket name is not configured. Set Aws:S3BucketName in appsettings.json or S3_BUCKET_NAME env var.");

        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        var httpClient = new HttpClient();
        _scraper = new DraftKingsScraper(httpClient, logger);

        var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(settings.Aws.Region));
        _s3Storage = new S3StorageService(s3Client, settings.Aws.S3BucketName, logger);

        // Register database context if connection string is configured
        if (!string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(settings.Database.ConnectionString)
                .Options;

            _dbStorage = new DbStorageService(new AppDbContext(dbOptions), logger);
        }
    }

    public async Task<string> FunctionHandler(ScrapeRequest input, ILambdaContext context)
    {
        context.Logger.LogInformation($"Scraping odds from: {input.Url}");

        try
        {
            var oddsData = await _scraper.ScrapeOddsAsync(input.Url);
            var s3Key = await _s3Storage.StoreOddsAsync(oddsData);

            return $"Success: stored odds at {s3Key}";
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error scraping odds: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }
}

public class ScrapeRequest
{
    public string Url { get; set; } = string.Empty;
}
