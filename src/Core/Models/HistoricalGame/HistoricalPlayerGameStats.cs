namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalPlayerGameStats
{
    // Player Info
    public string PlayerName { get; set; }
    public string Team { get; set; }

    // Passing Stats
    public int PassingCompletions { get; set; }
    public int PassingAttempts { get; set; }
    public int PassingYards { get; set; }
    public int PassingTouchdowns { get; set; }
    public int PassingInterceptions { get; set; }
    public int SacksTaken { get; set; }
    public int SackYardsLost { get; set; }
    public int PassingLong { get; set; }
    public double? PasserRating { get; set; }

    // Rushing Stats
    public int RushingAttempts { get; set; }
    public int RushingYards { get; set; }
    public int RushingTouchdowns { get; set; }
    public int RushingLong { get; set; }

    // Receiving Stats
    public int ReceivingTargets { get; set; }
    public int Receptions { get; set; }
    public int ReceivingYards { get; set; }
    public int ReceivingTouchdowns { get; set; }
    public int ReceivingLong { get; set; }

    // Fumbles
    public int Fumbles { get; set; }
    public int FumblesLost { get; set; }
}
