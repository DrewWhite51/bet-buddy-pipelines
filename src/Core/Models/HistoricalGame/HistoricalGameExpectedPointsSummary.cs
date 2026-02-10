namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalGameExpectedPointsSummary
{
    public string Team1 { get; set; }
    public string Team2 { get; set; }
    // Total expected points for the game
    public decimal Team1ExpectedPointsTotal { get; set; }
    public decimal Team2ExpectedPointsTotal { get; set; }

    // Total offensive expected points (rushing + passing + penalties)
    public decimal Team1ExpectedTot { get; set; } 
    public decimal Team2ExpectedTot { get; set; }
    // Expected points contributed by passing offense
    public decimal Team1ExpectedPass { get; set; }
    public decimal Team2ExpectedPass { get; set; }

    // Expected points contributed by rushing offense
    public decimal Team1ExpectedRush { get; set; }
    public decimal Team2ExpectedRush { get; set; }

    // Expected points contributed by turnovers on offense
    public decimal Team1ExpectedTOvr { get; set; }
    public decimal Team2ExpectedTOvr { get; set; }

    // Total defensive expected points (rushing defense + passing defense + defensive penalties)
    public decimal Team1ExpectedDefTot { get; set; }
    public decimal Team2ExpectedDefTot { get; set; }

    // Expected points contributed by passing defense
    public decimal Team1ExpectedDefPass { get; set; }
    public decimal Team2ExpectedDefPass { get; set; }

    // Expected points contributed by rushing defense
    public decimal Team1ExpectedDefRush { get; set; }
    public decimal Team2ExpectedDefRush { get; set; }

    // Expected points contributed by turnovers recovered on defense
    public decimal Team1ExpectedDefTOvr { get; set; }
    public decimal Team2ExpectedDefTOvr { get; set; }

    // All special teams expected points combined
    public decimal Team1SpecialTeamsExpectedTot { get; set; }
    public decimal Team2SpecialTeamsExpectedTot { get; set; }

    // Expected points contributed by kickoffs

    public decimal Team1KickoffExpected { get; set; }
    public decimal Team2KickoffExpected { get; set; }


    // Expected points contributed by kick return teams
    public decimal Team1KickReturnExpected { get; set; }
    public decimal Team2KickReturnExpected { get; set; }

    // Expected points contributed by punt teams
    public decimal Team1PuntExpected { get; set; }
    public decimal Team2PuntExpected { get; set; }

    // Expected points contributed by punt return teams
    public decimal Team1PuntReturnExpected { get; set; }
    public decimal Team2PuntReturnExpected { get; set; }


    // Expected points contributed by FG & XP kicking and defensive teams
    public decimal Team1FGXPKickingExpected { get; set; }
    public decimal Team2FGXPKickingExpected { get; set; }
    
}