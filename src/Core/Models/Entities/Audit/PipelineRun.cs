namespace SportsBettingPipeline.Core.Models.Entities.Audit;

public class PipelineRun
{
    public Guid Id { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Running";
    public string? ErrorMessage { get; set; }
    public int RecordsScraped { get; set; }
}
