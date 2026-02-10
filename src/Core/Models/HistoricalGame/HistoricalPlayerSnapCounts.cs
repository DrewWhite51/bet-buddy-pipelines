namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalPlayerSnapCounts
{
    // Player Info
    public string PlayerName { get; set; }
    public string Position { get; set; }
    public string Team { get; set; }

    // Offensive Snaps
    public int OffensiveSnaps { get; set; }
    public int OffensiveSnapsPercent { get; set; }

    // Defensive Snaps
    public int DefensiveSnaps { get; set; }
    public int DefensiveSnapsPercent { get; set; }

    // Special Teams Snaps
    public int SpecialTeamsSnaps { get; set; }
    public int SpecialTeamsSnapsPercent { get; set; }
}
