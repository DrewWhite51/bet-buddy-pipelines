# System Overview

## Current Architecture

```mermaid
graph LR
    Lambda[Lambda Function]
    Scraper[DraftKings Scraper]
    Site[DraftKings Website]
    S3[(S3 Bucket)]

    Lambda --> Scraper
    Scraper -->|HTTP GET| Site
    Scraper -->|OddsData| Lambda
    Lambda -->|JSON| S3
```

## Planned Architecture

```mermaid
graph LR
    EB[EventBridge Schedule]:::planned
    SQS[SQS Queue]:::planned
    Lambda[Lambda Function]
    DK[DraftKings Scraper]
    FD[FanDuel Scraper]:::planned
    BM[BetMGM Scraper]:::planned
    Sites[Sportsbook Sites]
    S3[(S3 Bucket)]
    RDS[(RDS Database)]:::planned

    EB -.->|cron trigger| SQS
    SQS -.->|event| Lambda
    Lambda --> DK
    Lambda -.-> FD
    Lambda -.-> BM
    DK -->|HTTP| Sites
    FD -.->|HTTP| Sites
    BM -.->|HTTP| Sites
    Lambda -->|JSON| S3
    Lambda -.->|structured data| RDS

    classDef planned stroke-dasharray: 5 5, stroke:#999, color:#999
```

## Components

| Component      | Status  | Description                                    |
|----------------|---------|------------------------------------------------|
| Lambda         | Active  | Entry point, orchestrates scrape + store        |
| DraftKings     | Active  | Scrapes DraftKings odds via HTML parsing        |
| S3 Storage     | Active  | Stores raw JSON odds snapshots                  |
| FanDuel        | Planned | Future scraper extending BaseScraperService     |
| BetMGM         | Planned | Future scraper extending BaseScraperService     |
| EventBridge    | Planned | Scheduled triggers for automated scraping       |
| SQS            | Planned | Decouples scheduling from execution             |
| RDS            | Planned | Structured storage for historical analysis      |
