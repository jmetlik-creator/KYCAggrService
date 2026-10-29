using Microsoft.EntityFrameworkCore;

namespace KycAggregation.Data;

public sealed class KycDbContext : DbContext
{
    public KycDbContext(DbContextOptions<KycDbContext> options) : base(options)
    {
    }

    public DbSet<PersistedCustomerKycData> CustomerKycRecords => Set<PersistedCustomerKycData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersistedCustomerKycData>(entity =>
        {
            entity.HasKey(x => x.Ssn);
            entity.Property(x => x.Ssn).HasMaxLength(32);
            entity.Property(x => x.FirstName).HasMaxLength(200);
            entity.Property(x => x.LastName).HasMaxLength(200);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.PhoneNumber).HasMaxLength(200);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.TaxCountry).HasMaxLength(10);
        });
    }
}
