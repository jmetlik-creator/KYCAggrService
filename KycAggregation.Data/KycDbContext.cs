using Microsoft.EntityFrameworkCore;

namespace KycAggregation.Data;

public sealed class KycDbContext : DbContext
{
    public KycDbContext(DbContextOptions<KycDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerPersonalDetailsRecord> CustomerPersonalDetails => Set<CustomerPersonalDetailsRecord>();
    public DbSet<CustomerContactDetailsRecord> CustomerContactDetails => Set<CustomerContactDetailsRecord>();
    public DbSet<CustomerKycFormRecord> CustomerKycForms => Set<CustomerKycFormRecord>();
    public DbSet<KycAggregationResultRecord> KycAggregationResults => Set<KycAggregationResultRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerPersonalDetailsRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ssn).HasMaxLength(32);
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.TransactionId);
            entity.HasIndex(x => x.Ssn);
        });

        modelBuilder.Entity<CustomerContactDetailsRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ssn).HasMaxLength(32);
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.TransactionId);
            entity.HasIndex(x => x.Ssn);
        });

        modelBuilder.Entity<CustomerKycFormRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ssn).HasMaxLength(32);
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.TransactionId);
            entity.HasIndex(x => x.Ssn);
        });

        modelBuilder.Entity<KycAggregationResultRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ssn).HasMaxLength(32);
            entity.Property(x => x.AggregatedPayloadJson).IsRequired();
            entity.Property(x => x.FetchState).HasMaxLength(50);
            entity.HasIndex(x => x.Ssn);
        });
    }
}
