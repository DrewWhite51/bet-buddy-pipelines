namespace SportsBettingPipeline.Core.Models.HistoricalGame;

public class HistoricalGameTeamStats
{
    public string Team1Abbreviation { get; set; }
    public string Team2Abbreviation { get; set; }

    public int Team1FirstDowns { get; set; }
    public int Team2FirstDowns { get; set; }

    public int Team1RushAttempts { get; set; }
    public int Team2RushAttempts { get; set; }

    public int Team1RushYards { get; set; }
    public int Team2RushYards { get; set; }

    public int Team1RushTouchdowns { get; set; }
    public int Team2RushTouchdowns { get; set; }

    public int Team1PassCompletions { get; set; }
    public int Team2PassCompletions { get; set; }

    public int Team1PassAttempts { get; set; }
    public int Team2PassAttempts { get; set; }

    public int Team1PassYards { get; set; }
    public int Team2PassYards { get; set; }

    public int Team1PassTouchdowns { get; set; }
    public int Team2PassTouchdowns { get; set; }

    public int Team1Interceptions { get; set; }
    public int Team2Interceptions { get; set; }

    public int Team1Sacks { get; set; }
    public int Team2Sacks { get; set; }

    public int Team1SackYards { get; set; }
    public int Team2SackYards { get; set; }

    public int Team1NetPassYards { get; set; }
    public int Team2NetPassYards { get; set; }

    public int Team1TotalYards { get; set; }
    public int Team2TotalYards { get; set; }

    public int Team1Fumbles { get; set; }
    public int Team2Fumbles { get; set; }

    public int Team1FumblesLost { get; set; }
    public int Team2FumblesLost { get; set; }

    public int Team1Turnovers { get; set; }
    public int Team2Turnovers { get; set; }

    public int Team1Penalties { get; set; }
    public int Team2Penalties { get; set; }


    public int Team1PenaltyYards { get; set; }
    public int Team2PenaltyYards { get; set; }


    public int Team1ThirdDownConversionAttempts { get; set; }
    public int Team2ThirdDownConversionAttempts { get; set; }

    public int Team1ThirdDownConversions { get; set; }
    public int Team2ThirdDownConversions { get; set; }

    public int Team1FourthDownConversionAttempts { get; set; }
    public int Team2FourthDownConversionAttempts { get; set; }

    public int Team1FourthDownConversions { get; set; }
    public int Team2FourthDownConversions { get; set; }

    public string Team1TimeOfPossession { get; set; }
    public string Team2TimeOfPossession { get; set; }

}