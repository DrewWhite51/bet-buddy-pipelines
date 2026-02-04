namespace SportsBettingPipeline.Core.Models;

public class AppSettings
{
    public AwsSettings Aws { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
}

public class AwsSettings
{
    public string S3BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}
