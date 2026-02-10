namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalGameDrive
{
    public string Team { get; set; }
    public int DriveNumber { get; set; }
    public int Quarter { get; set; }
    public string StartTime { get; set; }
    public string StartingFieldPosition { get; set; }
    public int Plays { get; set; }
    public string Duration { get; set; }
    public int NetYards { get; set; }
    public string Result { get; set; }
}
