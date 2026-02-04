# DraftKings Pipeline

## Overview

Scrapes betting odds from DraftKings Sportsbook and stores them as JSON in S3.

## Scraper

- **Class:** `SportsBettingPipeline.Scrapers.DraftKingsScraper`
- **Base:** `BaseScraperService`
- **Rate limit:** 1500ms between requests

## Target URLs

```
https://sportsbook.draftkings.com/leagues/football/nfl
```

<!-- Add specific event URL patterns as they are identified -->

## Fields Scraped

| Field       | Source                  | Notes                        |
|-------------|------------------------|------------------------------|
| Sportsbook  | Hardcoded `DraftKings` |                              |
| Sport       | Hardcoded `NFL`        | Extend for other sports      |
| Team1       | CSS `.event-cell-participant-name` (1st) | Placeholder selector |
| Team2       | CSS `.event-cell-participant-name` (2nd) | Placeholder selector |
| Spread      | CSS `.event-cell-spread`               | Placeholder selector |
| Moneyline   | CSS `.event-cell-moneyline`            | Placeholder selector |
| OverUnder   | CSS `.event-cell-total`                | Placeholder selector |
| Timestamp   | `DateTime.UtcNow` at scrape time       |                      |

## S3 Output

- **Key pattern:** `draftkings/{yyyy-MM-dd}/odds-{guid}.json`
- **Format:** Indented JSON

## Known Limitations

- CSS selectors are placeholders; need to inspect actual DraftKings HTML and update
- Only handles a single event per scrape invocation
- No JavaScript rendering (DraftKings may require a headless browser for full content)

## Future Work

- Add support for multiple sports (NBA, MLB, NHL)
- Handle paginated event lists
- Consider Playwright/Puppeteer if JS rendering is required
