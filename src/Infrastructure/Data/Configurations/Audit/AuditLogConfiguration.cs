using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBettingPipeline.Core.Models.Entities.Audit;

namespace SportsBettingPipeline.Infrastructure.Data.Configurations.Audit;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Action).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PipelineName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Message).HasMaxLength(2000);
        builder.Property(e => e.Timestamp).HasDefaultValueSql("now()");

        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.PipelineName);
    }
}
