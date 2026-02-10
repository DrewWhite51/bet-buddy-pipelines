# Historical Play-by-Play and Stats Pipeline

## Overview

This pipeline scrapes historical NFL game data from Pro Football Reference, extracting week-by-week game summaries and individual game details for analysis and sports betting insights.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Data Collection Flow                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  1. SCRAPE WEEKS              2. EXTRACT REFS           3. SCRAPE GAMES  │
│  ┌─────────────┐              ┌─────────────┐          ┌─────────────┐  │
│  │ Pro Football│   HTML       │  Week HTML  │   CSV    │  Game HTML  │  │
│  │  Reference  │ ─────────►   │   Parser    │ ──────►  │   Scraper   │  │
│  │  Week Pages │              │             │          │  (Future)   │  │
│  └─────────────┘              └─────────────┘          └─────────────┘  │
│        │                            │                        │          │
│        ▼                            ▼                        ▼          │
│  ┌─────────────┐              ┌─────────────┐          ┌─────────────┐  │
│  │     S3      │              │     S3      │          │     S3      │  │
│  │ weeks/      │              │ game-refs/  │          │ games/      │  │
│  │ unprocessed │              │ {year}/     │          │ unprocessed │  │
│  └─────────────┘              │ week{N}.csv │          │ (Future)    │  │
│                               └─────────────┘          └─────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

## S3 Directory Structure

```
pff-historical-games/
├── unprocessed/                    # Original week HTML (legacy path)
│   └── {year}/
│       └── week{N}.html
├── weeks/
│   └── processed/                  # Week HTML after extraction
│       └── {year}/
│           └── week{N}.html
├── game-references/                # Extracted game metadata CSVs
│   └── {year}/
│       └── week{N}.csv
└── games/                          # Individual game HTML (future)
    ├── unprocessed/
    │   └── {year}/
    │       └── {gameId}.html
    └── processed/
        └── {year}/
            └── {gameId}.html
```

## Game ID Format

Game IDs are extracted from Pro Football Reference boxscore URLs:

```
URL: /boxscores/202409050kan.htm
                ^^^^  ^^  ^^ ^^^
                │     │   │  └── Home team code (3 chars)
                │     │   └───── Day (2 digits)
                │     └───────── Month (2 digits)
                └─────────────── Year (4 digits)

GameId: 202409050kan
```

## CSV Output Format

Game reference CSVs contain:

```csv
GameId,Year,Week,GameDate,HomeTeamCode,BoxscoreUrl
202409050kan,2024,1,2024-09-05,kan,/boxscores/202409050kan.htm
202409060phi,2024,1,2024-09-06,phi,/boxscores/202409060phi.htm
```

## CLI Usage

### Scrape Week HTML to S3

First, scrape the raw week summary pages:

```bash
cd src

# Dry run - preview what will be scraped
dotnet run --project Sandbox -- --pff --year 2024 --dry-run

# Scrape single year
dotnet run --project Sandbox -- --pff --year 2024

# Scrape year range
dotnet run --project Sandbox -- --pff --from 2020 --to 2024
```

### Extract Game References

After week HTML is in S3, extract game references to CSV:

```bash
cd src

# Dry run - preview extraction
dotnet run --project Sandbox -- --pff-extract --year 2024 --dry-run

# Extract single week
dotnet run --project Sandbox -- --pff-extract --year 2024 --week 1

# Extract full year (moves week files to processed/)
dotnet run --project Sandbox -- --pff-extract --year 2024

# Extract without moving week files
dotnet run --project Sandbox -- --pff-extract --year 2024 --skip-move
```

### CLI Flags Reference

| Flag | Description |
|------|-------------|
| `--pff` | Scrape week summary HTML from Pro Football Reference |
| `--pff-extract` | Extract game references from week HTML to CSV |
| `--year, -y <year>` | Process a single year |
| `--from <year>` | Start year for range (default: 2000 for PFF) |
| `--to <year>` | End year for range (default: 2025) |
| `--week <N>` | Process specific week only (for --pff-extract) |
| `--skip-move` | Don't move week files to processed after extraction |
| `--dry-run, -d` | Preview without S3 writes |

## Key Components

### Models

| File | Description |
|------|-------------|
| `Core/Models/HistoricalGame/GameReference.cs` | Game reference with ID, date, team code, URL |
| `Core/Models/HistoricalGame/ExtractionResult.cs` | Result records for tracking extraction status |

### Scraper Methods

| Method | Description |
|--------|-------------|
| `PFFScraper.ScrapeYearToS3Async()` | Scrapes week HTML pages to S3 |
| `PFFScraper.ParseGameReferencesFromHtml()` | Parses boxscore links from week HTML |
| `PFFScraper.ExtractWeekToS3Async()` | Extracts game refs from one week, saves CSV |
| `PFFScraper.ExtractYearToS3Async()` | Extracts game refs for all weeks in a year |

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `S3_BUCKET_NAME` | S3 bucket for storing data | *(required)* |
| `AWS_REGION` | AWS region | us-east-1 |

## Processing Workflow

1. **Scrape Weeks** (`--pff`)
   - Fetches week summary pages from Pro Football Reference
   - Saves raw HTML to `pff-historical-games/unprocessed/{year}/week{N}.html`
   - Rate limited to 3 seconds between requests

2. **Extract Game References** (`--pff-extract`)
   - Reads week HTML from S3
   - Parses all boxscore links using AngleSharp
   - Generates unique game IDs from URLs
   - Saves CSV to `pff-historical-games/game-references/{year}/week{N}.csv`
   - Moves processed week HTML to `pff-historical-games/weeks/processed/`

3. **Scrape Games** (Future)
   - Read game reference CSVs
   - Fetch individual game detail pages
   - Save to `pff-historical-games/games/unprocessed/`

## Lambda Compatibility

The scraper methods are designed for Lambda deployment:

- All methods are stateless with explicit `s3Client` and `bucketName` parameters
- Processing is chunked by week (fits 15-min Lambda timeout)
- State is tracked via S3 paths (unprocessed vs processed)
- No local file dependencies

Future Lambda handler example:
```csharp
public async Task<string> Handler(S3Event s3Event, ILambdaContext context)
{
    // Triggered when week HTML is uploaded
    var s3Client = new AmazonS3Client();
    var bucket = s3Event.Records[0].S3.Bucket.Name;
    var key = s3Event.Records[0].S3.Object.Key;

    // Parse year/week from key
    var (year, week) = ParseKeyPath(key);

    // Extract game references
    var result = await pffScraper.ExtractWeekToS3Async(s3Client, bucket, year, week);
    return $"Extracted {result.GameCount} games";
}
```

## Current Implementation Status

- **Week Scraping**: Complete
- **Game Reference Extraction**: Complete
- **Game Detail Scraping**: Not started
- **Game Data Parsing**: Not started
- **Lambda Deployment**: Not started

## Future Enhancements

- Scrape individual game detail pages from boxscore URLs
- Parse game HTML into structured data models (drives, plays, stats)
- Lambda function triggered by S3 uploads
- Parallel game scraping with rate limit pool
- CloudWatch scheduled scraping for new games
