# Historical Lines Scraper - System Specification

## 1. Problem Statement
We need historical NFL betting data (spreads and over/unders) dating back to 1952 to support backtesting and analysis. Currently, this data exists on Covers.com but isn't in a usable format for our analytics pipeline.

## 2. Goals & Non-Goals

### Goals
- Backfill NFL betting data from 1952 to present
- Extract spreads and over/under lines from Covers.com
- Store data in structured CSV format in S3
- Enable one-time or ad-hoc execution for historical seasons

### Non-Goals
- Real-time or scheduled scraping (initially)
- Data for sports other than NFL
- Advanced betting metrics beyond spreads/over-unders
- Automated monitoring or alerting

## 3. User Flow
1. Developer runs scraper locally with target season year(s)
2. System fetches HTML from Covers.com for specified season
3. System extracts and parses HTML tables into structured data
4. System writes CSV files to designated S3 bucket
5. Developer verifies data quality in S3

## 4. Key Components
- **Web scraper**: Fetches HTML from Covers.com endpoint
- **HTML parser**: Extracts table data from page structure
- **CSV converter**: Transforms parsed data into CSV format
- **S3 writer**: Uploads files to cloud storage

## 5. Data & Integration Points

### Input
- Source: `https://www.covers.com/sportsoddshistory/nfl-game-season/?y=`
- Format: HTML tables containing game data, spreads, over/under lines
- URL pattern: Append year to base URL (e.g., `?y=1952`, `?y=2023`)

### Output
- Destination: S3 bucket
- Format: CSV files

### Integration
- AWS S3 for storage
- Local execution environment C# console program

## 6. Success Metrics
- Successfully retrieve data for all seasons (1952-present)
- CSV files are well-formed and parseable
- Data accuracy validated against source
- All files accessible in S3

## 7. Open Questions & Risks

### Questions
- What specific columns/fields should be extracted from each table?
- S3 bucket naming convention and folder structure?
- File naming convention (e.g., `nfl_lines_1952.csv`)?
- Error handling for missing seasons or malformed data?

### Risks
- Covers.com may change HTML structure, breaking parser
- Rate limiting or blocking from the source site
- Data completeness varies by season (earlier years may have gaps)
- No validation that scraped data matches source

## 8. Future Enhancements
- **Scheduled execution**: Deploy as annual Lambda function to capture new seasons automatically
- **Incremental updates**: Only scrape new games rather than full season rescans
- **Data validation**: Add checks against known results or alternative sources
- **Multi-sport support**: Extend to NBA, MLB, etc.
- **Monitoring**: CloudWatch alerts for scraper failures

## 9. Current Implementation Status
- **Status**: Local execution only
- **Environment**: C# console application (AngleSharp, AWS SDK, Serilog)
- **Deployment**: Not automated; manual runs as needed
- **Testing**: TBD

---

## Notes
- Initial focus is on one-time backfill; automation can be added if recurring needs emerge
- Consider documenting the HTML table structure in case of future changes