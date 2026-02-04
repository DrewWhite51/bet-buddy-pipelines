namespace SportsBettingPipeline.Core.Models.Entities.Odds;

public class OddsRecord
{
    public Guid Id { get; set; }
    public string Sportsbook { get; set; } = string.Empty;
    public string Sport { get; set; } = string.Empty;
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public decimal? Spread { get; set; }
    public decimal? Moneyline { get; set; }
    public decimal? OverUnder { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime ScrapedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
