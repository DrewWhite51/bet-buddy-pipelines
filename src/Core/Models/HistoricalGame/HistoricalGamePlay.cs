namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalGamePlay
{
    public int Quarter { get; set; }
    public string Time { get; set; }
    public int? Down { get; set; }
    public int? ToGo { get; set; }
    public string Location { get; set; }
    public int AwayScore { get; set; }
    public int HomeScore { get; set; }
    public string Detail { get; set; }
    public double ExpectedPointsBefore { get; set; }
    public double ExpectedPointsAdded { get; set; }
}
