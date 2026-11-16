using Microsoft.EntityFrameworkCore;
using NeoWatch.Core.Models;

namespace NeoWatch.Infrastructure;

public sealed class NeoWatchDbContext(DbContextOptions<NeoWatchDbContext> options) : DbContext(options)
{
    public DbSet<Asteroid> Asteroids => Set<Asteroid>();
    public DbSet<CloseApproach> CloseApproaches => Set<CloseApproach>();
    public DbSet<IngestionRun> IngestionRuns => Set<IngestionRun>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The unique indexes are the database-level guarantee behind idempotency:
        // even if the upsert code has a bug, duplicates cannot be written.
        modelBuilder.Entity<Asteroid>(e =>
        {
            e.HasIndex(a => a.NeoId).IsUnique();
            e.Property(a => a.NeoId).HasMaxLength(32);
            e.Property(a => a.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<CloseApproach>(e =>
        {
            e.HasIndex(c => new { c.AsteroidId, c.EpochMillis }).IsUnique();
            e.HasIndex(c => c.Date);
            e.HasOne(c => c.Asteroid)
                .WithMany(a => a.CloseApproaches)
                .HasForeignKey(c => c.AsteroidId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailySummary>(e => e.HasKey(s => s.Date));

        modelBuilder.Entity<IngestionRun>(e => e.HasIndex(r => new { r.WindowStart, r.WindowEnd }));
    }
}
