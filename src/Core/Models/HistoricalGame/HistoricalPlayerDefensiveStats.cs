namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalPlayerDefensiveStats
{
    // Player Info
    public string PlayerName { get; set; }
    public string Team { get; set; }

    // Defensive Interceptions
    public int Interceptions { get; set; }
    public int InterceptionYards { get; set; }
    public int InterceptionTouchdowns { get; set; }
    public int InterceptionLong { get; set; }
    public int PassesDefended { get; set; }

    // Tackles
    public double Sacks { get; set; }
    public int TacklesCombined { get; set; }
    public int TacklesSolo { get; set; }
    public int TacklesAssisted { get; set; }
    public int TacklesForLoss { get; set; }
    public int QBHits { get; set; }

    // Fumbles
    public int FumbleRecoveries { get; set; }
    public int FumbleRecoveryYards { get; set; }
    public int FumbleRecoveryTouchdowns { get; set; }
    public int ForcedFumbles { get; set; }
}
