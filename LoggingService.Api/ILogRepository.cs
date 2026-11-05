using Logging.Contracts;

namespace LoggingService.Api;

public interface ILogRepository
{
    Task AddAsync(LogEntry entry, CancellationToken cancellationToken = default);
    Task<List<LogEntry>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<List<LogEntry>> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
}
