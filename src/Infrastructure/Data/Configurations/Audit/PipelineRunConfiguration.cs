using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBettingPipeline.Core.Models.Entities.Audit;

namespace SportsBettingPipeline.Infrastructure.Data.Configurations.Audit;

public class PipelineRunConfiguration : IEntityTypeConfiguration<PipelineRun>
{
    public void Configure(EntityTypeBuilder<PipelineRun> builder)
    {
        builder.ToTable("pipeline_runs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.PipelineName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.StartedAt).IsRequired();

        builder.HasIndex(e => new { e.PipelineName, e.StartedAt });
        builder.HasIndex(e => e.Status);
    }
}
