using SportsBettingPipeline.Core.Models.Entities.Audit;
using SportsBettingPipeline.Core.Models.Entities.Odds;

namespace SportsBettingPipeline.Core.Storage;

public interface IDbStorage
{
    Task<Guid> StoreOddsRecordAsync(OddsRecord record);
    Task<Guid> LogPipelineRunAsync(PipelineRun run);
    Task UpdatePipelineRunAsync(PipelineRun run);
}
