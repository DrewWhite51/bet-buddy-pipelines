using AngleSharp.Html.Dom;
using Serilog;
using SportsBettingPipeline.Core.Models;
using SportsBettingPipeline.Core.Scrapers;

namespace SportsBettingPipeline.Scrapers;

public class DraftKingsScraper : BaseScraperService
{
    private const string SportsbookName = "DraftKings";

    public DraftKingsScraper(HttpClient httpClient, ILogger logger)
        : base(httpClient, logger, rateLimitDelayMs: 1500)
    {
    }

    protected override OddsData ParseHtml(IHtmlDocument document)
    {
        // TODO: Update selectors to match actual DraftKings page structure.
        // These are placeholder selectors that should be refined once we
        // inspect the real HTML of the target pages.

        var teams = document.QuerySelectorAll(".event-cell-participant-name");
        var spreads = document.QuerySelectorAll(".event-cell-spread");
        var moneylines = document.QuerySelectorAll(".event-cell-moneyline");
        var overUnders = document.QuerySelectorAll(".event-cell-total");

        var team1 = teams.Length > 0 ? teams[0].TextContent.Trim() : "Unknown";
        var team2 = teams.Length > 1 ? teams[1].TextContent.Trim() : "Unknown";

        return new OddsData
        {
            Sportsbook = SportsbookName,
            Sport = "NFL", // Default for now; extend later
            Team1 = team1,
            Team2 = team2,
            Spread = TryParseDecimal(spreads.Length > 0 ? spreads[0].TextContent : null),
            Moneyline = TryParseDecimal(moneylines.Length > 0 ? moneylines[0].TextContent : null),
            OverUnder = TryParseDecimal(overUnders.Length > 0 ? overUnders[0].TextContent : null),
            Timestamp = DateTime.UtcNow
        };
    }

    private static decimal? TryParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Strip non-numeric characters except minus and decimal point
        var cleaned = new string(value.Where(c => char.IsDigit(c) || c == '-' || c == '.').ToArray());
        return decimal.TryParse(cleaned, out var result) ? result : null;
    }
}
