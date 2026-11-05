using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Logging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LoggingService.Api;

public sealed class LogRepository : ILogRepository
{
    private readonly LogDbContext _context;
    private readonly TimeSpan _dedupeWindow = TimeSpan.FromMinutes(5);

    public LogRepository(LogDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        entry.Fingerprint = ComputeFingerprint(entry);
        var cutoff = DateTime.UtcNow.Subtract(_dedupeWindow);

        var isDuplicate = await _context.LogEntries
            .AnyAsync(x => x.Fingerprint == entry.Fingerprint && x.CreatedUtc >= cutoff, cancellationToken)
            .ConfigureAwait(false);

        if (isDuplicate)
        {
            return;
        }

        _context.LogEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<LogEntry>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.LogEntries
            .OrderByDescending(x => x.CreatedUtc)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<LogEntry>> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return new List<LogEntry>();
        }

        var normalizedTransactionId = transactionId.Trim();

        return await _context.LogEntries
            .Where(x => x.CorrelationId == normalizedTransactionId)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string ComputeFingerprint(LogEntry entry)
    {
        var payload = JsonSerializer.Serialize(new
        {
            entry.EventName,
            entry.ServiceName,
            entry.Level,
            entry.Message,
            entry.Exception,
            entry.CorrelationId,
            Context = entry.Context.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hashBytes);
    }
}
