# Data Flow

## Scrape Pipeline Sequence

```mermaid
sequenceDiagram
    participant Client as Caller (manual / EventBridge)
    participant Lambda as Lambda Function
    participant Scraper as DraftKingsScraper
    participant Site as DraftKings Website
    participant S3 as S3 Bucket

    Client->>Lambda: Invoke with { url }
    Lambda->>Scraper: ScrapeOddsAsync(url)
    Scraper->>Scraper: Rate limit delay (1500ms)
    Scraper->>Site: HTTP GET url
    Site-->>Scraper: HTML response
    Scraper->>Scraper: Parse HTML → OddsData
    Scraper-->>Lambda: OddsData
    Lambda->>S3: PutObject (JSON)
    S3-->>Lambda: Success
    Lambda-->>Client: "Success: stored odds at {key}"
```

## Data Transformations

```mermaid
flowchart LR
    HTML[Raw HTML] -->|AngleSharp parse| DOM[DOM Document]
    DOM -->|CSS selectors| Fields[Extracted fields]
    Fields -->|Map to POCO| OddsData[OddsData object]
    OddsData -->|System.Text.Json| JSON[JSON string]
    JSON -->|PutObject| S3[S3 object]
```

## S3 Key Structure

```
s3://{S3_BUCKET_NAME}/
  └── draftkings/
      └── 2026-01-29/
          ├── odds-a1b2c3d4.json
          ├── odds-e5f6g7h8.json
          └── ...
```
