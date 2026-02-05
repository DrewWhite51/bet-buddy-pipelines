using AngleSharp.Html.Dom;
using Serilog;
using SportsBettingPipeline.Core.Models;
using SportsBettingPipeline.Core.Scrapers;

namespace SportsBettingPipeline.Scrapers;

public class PFFScraper : BaseScraperService
{
    private const string BaseUrl = "https://www.pff.com/"; // TODO: Update with actual URL
    private const string SportsbookName = "PFF";

    public PFFScraper(HttpClient httpClient, ILogger logger)
        : base(httpClient, logger, rateLimitDelayMs: 1500)
    {
        
    }

    protected override OddsData ParseHtml(IHtmlDocument document)
    {
        // TODO: Implement parsing logic
        return new OddsData();
    }

    public string HelloPFFScraper()
    {
        return "Hello from PFFScraper!";
    }

    public async Task<string> DumpHtmlAsync(string url)
    {
        Logger.Information("Fetching HTML from {Url}", url);
        var html = await HttpClient.GetStringAsync(url);
        Logger.Information("Fetched {Length} chars", html.Length);
        Console.WriteLine(html);
        return html;
    }

    public async Task<string> DumpHtmlToFileAsync(string url, string? outputPath = null)
    {
        var html = await HttpClient.GetStringAsync(url);
        outputPath ??= Path.Combine(Path.GetTempPath(), "pff-dump.html");
        await File.WriteAllTextAsync(outputPath, html);
        Logger.Information("HTML written to {Path} ({Length} chars)", outputPath, html.Length);
        return outputPath;
    }
}
