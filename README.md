# Sports Betting Pipeline

A .NET 8 solution for scraping sports betting odds and storing them in AWS S3, deployed as an AWS Lambda function.

## Project Structure

```
SportsBettingPipeline/
├── src/
│   ├── SportsBettingPipeline.sln
│   ├── Core/                          # Models, interfaces, base classes
│   │   ├── Models/OddsData.cs         # Odds data POCO
│   │   ├── Scrapers/                  # IScraperService + BaseScraperService
│   │   └── Storage/IS3Storage.cs      # Storage interface
│   ├── Scrapers/                      # Concrete scraper implementations
│   │   └── DraftKingsScraper.cs
│   ├── Infrastructure/                # AWS service implementations
│   │   └── S3StorageService.cs
│   └── Lambda/                        # AWS Lambda entry point
│       ├── Function.cs
│       └── aws-lambda-tools-defaults.json
└── tests/
    └── SportsBettingPipeline.Tests/
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS CLI](https://aws.amazon.com/cli/) configured with valid credentials
- [Amazon.Lambda.Tools](https://github.com/aws/aws-extensions-for-dotnet-cli) global tool:
  ```bash
  dotnet tool install -g Amazon.Lambda.Tools
  ```

## Build

```bash
cd SportsBettingPipeline/src
dotnet restore
dotnet build
```

## Run Tests

```bash
cd SportsBettingPipeline
dotnet test
```

## Deploy to AWS Lambda

```bash
cd SportsBettingPipeline/src/Lambda
dotnet lambda deploy-function
```

## Environment Variables

| Variable        | Description                       | Default     |
|-----------------|-----------------------------------|-------------|
| `S3_BUCKET_NAME`| S3 bucket for storing scraped data| *(required)*|
| `AWS_REGION`    | AWS region                        | us-east-1   |

## Invoking the Lambda

The Lambda expects a JSON payload with a `url` field:

```json
{
  "url": "https://sportsbook.draftkings.com/..."
}
```

Test invoke via AWS CLI:

```bash
aws lambda invoke \
  --function-name SportsBettingPipeline-Scraper \
  --payload '{"url": "https://sportsbook.draftkings.com/..."}' \
  output.json
```

## Adding New Scrapers

1. Create a new class in `src/Scrapers/` that extends `BaseScraperService`
2. Implement the `ParseHtml(IHtmlDocument document)` method with site-specific selectors
3. Wire it up in `Function.cs` or via dependency injection

## Flow

```
Lambda triggered → DraftKingsScraper scrapes page → OddsData saved to S3
```
