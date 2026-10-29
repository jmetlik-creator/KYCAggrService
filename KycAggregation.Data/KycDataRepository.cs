using Microsoft.EntityFrameworkCore;

namespace KycAggregation.Data;

public sealed class KycDataRepository : IKycDataRepository
{
    private readonly KycDbContext _context;

    public KycDataRepository(KycDbContext context)
    {
        _context = context;
    }

    public async Task<PersistedCustomerKycData?> GetBySsnAsync(string ssn, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(ssn);

        return await _context.CustomerKycRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ssn == normalizedSsn, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(PersistedCustomerKycData record, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(record.Ssn);
        var existing = await _context.CustomerKycRecords
            .FirstOrDefaultAsync(x => x.Ssn == normalizedSsn, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            record.Ssn = normalizedSsn;
            record.CreatedUtc = DateTime.UtcNow;
            record.UpdatedUtc = record.CreatedUtc;
            _context.CustomerKycRecords.Add(record);
        }
        else
        {
            existing.FirstName = record.FirstName;
            existing.LastName = record.LastName;
            existing.Address = record.Address;
            existing.PhoneNumber = record.PhoneNumber;
            existing.Email = record.Email;
            existing.TaxCountry = record.TaxCountry;
            existing.Income = record.Income;
            existing.UpdatedUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeSsn(string ssn)
    {
        return ssn.Trim();
    }
}
