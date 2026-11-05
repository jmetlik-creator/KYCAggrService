using Microsoft.EntityFrameworkCore;

namespace KycAggregation.Data;

public sealed class KycDataRepository : IKycDataRepository
{
    private readonly KycDbContext _context;

    public KycDataRepository(KycDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerPersonalDetailsRecord?> GetPersonalDetailsAsync(string ssn, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(ssn);
        return await _context.CustomerPersonalDetails
            .AsNoTracking()
            .Where(x => x.Ssn == normalizedSsn)
            .OrderByDescending(x => x.FetchedUtc)
            .ThenByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SavePersonalDetailsAsync(CustomerPersonalDetailsRecord record, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(record.Ssn);
        record.Ssn = normalizedSsn;
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        record.CreatedUtc = record.CreatedUtc == default ? DateTime.UtcNow : record.CreatedUtc;

        _context.CustomerPersonalDetails.Add(record);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerContactDetailsRecord?> GetContactDetailsAsync(string ssn, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(ssn);
        return await _context.CustomerContactDetails
            .AsNoTracking()
            .Where(x => x.Ssn == normalizedSsn)
            .OrderByDescending(x => x.FetchedUtc)
            .ThenByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveContactDetailsAsync(CustomerContactDetailsRecord record, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(record.Ssn);
        record.Ssn = normalizedSsn;
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        record.CreatedUtc = record.CreatedUtc == default ? DateTime.UtcNow : record.CreatedUtc;

        _context.CustomerContactDetails.Add(record);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerKycFormRecord?> GetKycFormAsync(string ssn, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(ssn);
        return await _context.CustomerKycForms
            .AsNoTracking()
            .Where(x => x.Ssn == normalizedSsn)
            .OrderByDescending(x => x.FetchedUtc)
            .ThenByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveKycFormAsync(CustomerKycFormRecord record, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(record.Ssn);
        record.Ssn = normalizedSsn;
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        record.CreatedUtc = record.CreatedUtc == default ? DateTime.UtcNow : record.CreatedUtc;

        _context.CustomerKycForms.Add(record);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<KycAggregationResultRecord?> GetLatestAggregationAsync(string ssn, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeSsn(ssn);
        return await _context.KycAggregationResults
            .AsNoTracking()
            .Where(x => x.Ssn == normalizedSsn)
            .OrderByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAggregationAsync(KycAggregationResultRecord record, CancellationToken cancellationToken = default)
    {
        record.Ssn = NormalizeSsn(record.Ssn);
        record.CreatedUtc = DateTime.UtcNow;
        _context.KycAggregationResults.Add(record);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeSsn(string ssn)
    {
        return ssn.Trim();
    }
}
