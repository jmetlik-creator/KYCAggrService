using Logging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LoggingService.Api;

public sealed class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
    {
    }

    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventName).HasMaxLength(200);
            entity.Property(x => x.ServiceName).HasMaxLength(200);
            entity.Property(x => x.Level).HasMaxLength(50);
            entity.Property(x => x.Message).HasMaxLength(4000);
            entity.Property(x => x.Exception).HasMaxLength(4000);
            entity.Property(x => x.CorrelationId).HasMaxLength(200);
            entity.Property(x => x.Fingerprint).HasMaxLength(128);
            entity.Property(x => x.ContextJson).HasMaxLength(4000);

            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.Fingerprint);
            entity.HasIndex(x => new { x.ServiceName, x.EventName, x.Level, x.Message });
        });
    }
}
