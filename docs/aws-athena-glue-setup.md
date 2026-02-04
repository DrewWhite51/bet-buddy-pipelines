# AWS Athena & Glue Integration

This guide covers setting up AWS Athena and Glue to query the odds data stored in S3 using standard SQL.

## Overview

| Service | Purpose |
|---------|---------|
| **S3** | Stores raw JSON odds data from scrapers |
| **Glue Crawler** | Automatically discovers schema from S3 data and creates a table in the Glue Data Catalog |
| **Glue Data Catalog** | Metadata store that Athena uses to understand the structure of your data |
| **Athena** | Serverless SQL query engine that queries data directly in S3 |

```
S3 (JSON files) → Glue Crawler → Glue Data Catalog → Athena (SQL queries)
```

## Why This Stack?

- **Serverless** — no infrastructure to manage, pay only for what you use
- **Cost-effective** — Athena charges $5 per TB scanned; Glue crawlers are cheap for small datasets
- **SQL interface** — query odds data without loading it into a database
- **Scales automatically** — handles everything from kilobytes to petabytes

---

## Step 1: Organize S3 Data

Your current S3 key structure already works well:

```
s3://your-bucket/
  └── draftkings/
      └── 2026-01-29/
          ├── odds-abc123.json
          ├── odds-def456.json
          └── ...
```

Athena works best with **partitioned data**. Your structure is already partitioned by sportsbook and date, which enables efficient querying.

---

## Step 2: Create a Glue Database

1. Go to **AWS Glue** in the console
2. Click **Databases** in the left sidebar
3. Click **Add database**
4. Name: `sportsbetting`
5. Click **Create database**

---

## Step 3: Create a Glue Crawler

The crawler will scan your S3 bucket and infer the schema from your JSON files.

1. Go to **Crawlers** in the left sidebar
2. Click **Create crawler**
3. Name: `sportsbetting-odds-crawler`
4. Click **Next**

### Data source configuration
5. Click **Add a data source**
6. Data source: **S3**
7. S3 path: `s3://your-bucket/draftkings/` (or just `s3://your-bucket/` for all sportsbooks)
8. Crawl all sub-folders: **Yes**
9. Click **Add an S3 data source**, then **Next**

### IAM role
10. Choose **Create new IAM role**
11. Name: `AWSGlueServiceRole-sportsbetting`
12. Click **Next**

### Output configuration
13. Target database: `sportsbetting`
14. Table name prefix: `odds_` (optional)
15. Crawler schedule: **On demand** (for now)
16. Click **Next**, then **Create crawler**

### Run the crawler
17. Select your crawler and click **Run**
18. Wait for it to complete (usually under a minute for small datasets)
19. Check the **Tables** section — you should see a new table

---

## Step 4: Query with Athena

1. Go to **Amazon Athena** in the console
2. If first time: set up a query result location
   - Click **Settings** → **Manage**
   - S3 location: `s3://your-bucket/athena-results/`
   - Click **Save**

3. Select database: `sportsbetting`
4. Run a query:

```sql
-- Preview the data
SELECT * FROM odds_draftkings LIMIT 10;

-- Count records by date
SELECT
    date_format(from_iso8601_timestamp(timestamp), '%Y-%m-%d') as date,
    COUNT(*) as record_count
FROM odds_draftkings
GROUP BY 1
ORDER BY 1 DESC;

-- Find all Chiefs games
SELECT *
FROM odds_draftkings
WHERE team1 LIKE '%Chiefs%' OR team2 LIKE '%Chiefs%';

-- Average spread by team
SELECT
    team1,
    AVG(spread) as avg_spread,
    COUNT(*) as games
FROM odds_draftkings
WHERE spread IS NOT NULL
GROUP BY team1
ORDER BY avg_spread;
```

---

## Step 5: Optimize for Cost (Optional)

### Convert to Parquet

JSON is inefficient for Athena (scans entire files). Convert to Parquet for 10-100x cost savings:

```sql
-- Create a new table in Parquet format
CREATE TABLE odds_draftkings_parquet
WITH (
    format = 'PARQUET',
    external_location = 's3://your-bucket/parquet/draftkings/',
    partitioned_by = ARRAY['scrape_date']
) AS
SELECT
    sportsbook,
    sport,
    team1,
    team2,
    spread,
    moneyline,
    overunder,
    timestamp,
    date_format(from_iso8601_timestamp(timestamp), '%Y-%m-%d') as scrape_date
FROM odds_draftkings;
```

### Add partitions

If you partition by date, Athena only scans the partitions you query:

```sql
-- Query only January 2026 data
SELECT * FROM odds_draftkings_parquet
WHERE scrape_date BETWEEN '2026-01-01' AND '2026-01-31';
```

---

## Step 6: Automate the Crawler (Optional)

To keep the schema up to date as new data arrives:

1. Go to your crawler
2. Click **Edit**
3. Set schedule: **Daily** or use a cron expression
4. Alternatively, trigger the crawler from your Lambda after writing to S3

### Trigger from Lambda (code snippet)

```csharp
using Amazon.Glue;
using Amazon.Glue.Model;

var glueClient = new AmazonGlueClient();
await glueClient.StartCrawlerAsync(new StartCrawlerRequest
{
    Name = "sportsbetting-odds-crawler"
});
```

Add `AWSSDK.Glue` to your Lambda project if using this approach.

---

## Cost Estimates

| Service | Cost |
|---------|------|
| **Glue Crawler** | $0.44 per DPU-hour (typically < $0.01 per run for small datasets) |
| **Glue Data Catalog** | Free for first 1M objects, then $1 per 100K objects |
| **Athena** | $5 per TB scanned (Parquet reduces this significantly) |
| **S3** | Standard storage rates |

For a small project, expect < $1/month total.

---

## Project Integration Ideas

1. **Daily analytics job** — Use Athena to generate daily summary reports
2. **Historical analysis** — Query trends across months of data
3. **Data validation** — Check for anomalies in scraped data
4. **Dashboard backend** — Connect Athena to QuickSight or Grafana

---

## References

### Official Documentation
- [AWS Glue Developer Guide](https://docs.aws.amazon.com/glue/latest/dg/what-is-glue.html)
- [AWS Athena User Guide](https://docs.aws.amazon.com/athena/latest/ug/what-is.html)
- [Glue Crawler Tutorial](https://docs.aws.amazon.com/glue/latest/dg/add-crawler.html)
- [Athena SQL Reference](https://docs.aws.amazon.com/athena/latest/ug/ddl-sql-reference.html)

### Best Practices
- [Athena Performance Tuning](https://docs.aws.amazon.com/athena/latest/ug/performance-tuning.html)
- [Partitioning Data in Athena](https://docs.aws.amazon.com/athena/latest/ug/partitions.html)
- [Converting to Columnar Formats](https://docs.aws.amazon.com/athena/latest/ug/convert-to-columnar.html)

### Tutorials
- [Querying JSON Data in Athena](https://docs.aws.amazon.com/athena/latest/ug/querying-json.html)
- [Using Glue Crawlers](https://docs.aws.amazon.com/glue/latest/dg/crawler-running.html)
- [Athena + Glue Workshop](https://catalog.us-east-1.prod.workshops.aws/workshops/9981f1a1-abdc-49b5-8387-cb01d238bb78/en-US)

### .NET SDK
- [AWS SDK for .NET - Glue](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Glue/NGlueModel.html)
- [AWS SDK for .NET - Athena](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Athena/NAthenaModel.html)
