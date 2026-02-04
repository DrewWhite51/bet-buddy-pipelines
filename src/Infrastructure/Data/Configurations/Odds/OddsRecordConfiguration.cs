using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBettingPipeline.Core.Models.Entities.Odds;

namespace SportsBettingPipeline.Infrastructure.Data.Configurations.Odds;

public class OddsRecordConfiguration : IEntityTypeConfiguration<OddsRecord>
{
    public void Configure(EntityTypeBuilder<OddsRecord> builder)
    {
        builder.ToTable("odds_records");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Sportsbook).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Sport).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Team1).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Team2).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Spread).HasPrecision(10, 2);
        builder.Property(e => e.Moneyline).HasPrecision(10, 2);
        builder.Property(e => e.OverUnder).HasPrecision(10, 2);
        builder.Property(e => e.SourceUrl).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.Sportsbook, e.ScrapedAt });
        builder.HasIndex(e => new { e.Team1, e.Team2, e.ScrapedAt });
    }
}
