namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalPlayerReturnStats
{
    // Player Info
    public string PlayerName { get; set; }
    public string Team { get; set; }

    // Kick Returns
    public int KickReturns { get; set; }
    public int KickReturnYards { get; set; }
    public double? KickReturnYardsPerReturn { get; set; }
    public int KickReturnTouchdowns { get; set; }
    public int KickReturnLong { get; set; }

    // Punt Returns
    public int PuntReturns { get; set; }
    public int PuntReturnYards { get; set; }
    public double? PuntReturnYardsPerReturn { get; set; }
    public int PuntReturnTouchdowns { get; set; }
    public int PuntReturnLong { get; set; }
}
