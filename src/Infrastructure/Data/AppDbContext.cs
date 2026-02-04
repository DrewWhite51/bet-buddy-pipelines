using Microsoft.EntityFrameworkCore;
using SportsBettingPipeline.Core.Models.Entities.Audit;
using SportsBettingPipeline.Core.Models.Entities.Odds;

namespace SportsBettingPipeline.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<OddsRecord> OddsRecords => Set<OddsRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
