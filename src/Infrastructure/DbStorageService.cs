using Serilog;
using SportsBettingPipeline.Core.Models.Entities.Audit;
using SportsBettingPipeline.Core.Models.Entities.Odds;
using SportsBettingPipeline.Core.Storage;
using SportsBettingPipeline.Infrastructure.Data;

namespace SportsBettingPipeline.Infrastructure;

public class DbStorageService : IDbStorage
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public DbStorageService(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid> StoreOddsRecordAsync(OddsRecord record)
    {
        if (record.Id == Guid.Empty)
            record.Id = Guid.NewGuid();

        record.CreatedAt = DateTime.UtcNow;

        _db.OddsRecords.Add(record);
        await _db.SaveChangesAsync();

        _logger.Information("Stored odds record {Id} for {Team1} vs {Team2}", record.Id, record.Team1, record.Team2);

        return record.Id;
    }

    public async Task<Guid> LogPipelineRunAsync(PipelineRun run)
    {
        if (run.Id == Guid.Empty)
            run.Id = Guid.NewGuid();

        _db.PipelineRuns.Add(run);
        await _db.SaveChangesAsync();

        _logger.Information("Logged pipeline run {Id} for {Pipeline}", run.Id, run.PipelineName);

        return run.Id;
    }

    public async Task UpdatePipelineRunAsync(PipelineRun run)
    {
        _db.PipelineRuns.Update(run);
        await _db.SaveChangesAsync();

        _logger.Information("Updated pipeline run {Id}: {Status}", run.Id, run.Status);
    }
}
