namespace SportsBettingPipeline.Core.Models;

public class OddsData
{
    public string Sportsbook { get; set; } = string.Empty;
    public string Sport { get; set; } = string.Empty;
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public decimal? Spread { get; set; }
    public decimal? Moneyline { get; set; }
    public decimal? OverUnder { get; set; }
    public DateTime Timestamp { get; set; }
}
