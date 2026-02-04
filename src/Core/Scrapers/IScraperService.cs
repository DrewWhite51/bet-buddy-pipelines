using SportsBettingPipeline.Core.Models;

namespace SportsBettingPipeline.Core.Scrapers;

public interface IScraperService
{
    Task<OddsData> ScrapeOddsAsync(string url);
}
