using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Serilog;
using SportsBettingPipeline.Core.Models;

namespace SportsBettingPipeline.Core.Scrapers;

public abstract class BaseScraperService : IScraperService
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;

    private readonly int _rateLimitDelayMs;

    protected BaseScraperService(HttpClient httpClient, ILogger logger, int rateLimitDelayMs = 1000)
    {
        HttpClient = httpClient;
        Logger = logger;
        _rateLimitDelayMs = rateLimitDelayMs;
    }

    public async Task<OddsData> ScrapeOddsAsync(string url)
    {
        Logger.Information("Scraping odds from {Url}", url);

        await RateLimitDelay();

        var html = await HttpClient.GetStringAsync(url);

        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html);

        var oddsData = ParseHtml(document);

        Logger.Information("Successfully scraped odds for {Team1} vs {Team2}", oddsData.Team1, oddsData.Team2);

        return oddsData;
    }

    protected abstract OddsData ParseHtml(IHtmlDocument document);

    protected async Task RateLimitDelay()
    {
        if (_rateLimitDelayMs > 0)
        {
            Logger.Debug("Rate limiting: waiting {Delay}ms", _rateLimitDelayMs);
            await Task.Delay(_rateLimitDelayMs);
        }
    }
}
