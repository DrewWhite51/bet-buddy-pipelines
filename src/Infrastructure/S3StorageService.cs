using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Serilog;
using SportsBettingPipeline.Core.Models;
using SportsBettingPipeline.Core.Storage;

namespace SportsBettingPipeline.Infrastructure;

public class S3StorageService : IS3Storage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger _logger;

    public S3StorageService(IAmazonS3 s3Client, string bucketName, ILogger logger)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
        _logger = logger;
    }

    public async Task<string> StoreOddsAsync(OddsData oddsData)
    {
        var json = JsonSerializer.Serialize(oddsData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var key = $"{oddsData.Sportsbook.ToLowerInvariant()}/{oddsData.Timestamp:yyyy-MM-dd}/odds-{Guid.NewGuid()}.json";

        _logger.Information("Storing odds data to s3://{Bucket}/{Key}", _bucketName, key);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            ContentBody = json,
            ContentType = "application/json"
        };

        await _s3Client.PutObjectAsync(request);

        _logger.Information("Successfully stored odds data at {Key}", key);

        return key;
    }
}
