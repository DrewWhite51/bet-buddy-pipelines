namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalPlayerKickingStats
{
    // Player Info
    public string PlayerName { get; set; }
    public string Team { get; set; }

    // Scoring (Field Goals / Extra Points)
    public int? ExtraPointsMade { get; set; }
    public int? ExtraPointsAttempted { get; set; }
    public int? FieldGoalsMade { get; set; }
    public int? FieldGoalsAttempted { get; set; }

    // Punting
    public int Punts { get; set; }
    public int PuntYards { get; set; }
    public double? PuntYardsPerPunt { get; set; }
    public int PuntLong { get; set; }
}
