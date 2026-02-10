namespace SportsBettingPipeline.Core.Models.HistoricalGame;


// This class is based off of the scoring data that are found on these pages: https://www.pro-football-reference.com/boxscores/201601160nwe.htm
public class HistoricalGameScoringData
{
    public int Quarter { get; set; }
    public string Time { get; set; }
    public string ScoringTeam {get; set; }

    public string PlayDetail { get; set; }

    public string Team1Score { get; set; }

    public string Team2Score { get; set; }
}