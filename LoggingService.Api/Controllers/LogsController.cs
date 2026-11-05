using Logging.Contracts;
using LoggingService.Api;
using Microsoft.AspNetCore.Mvc;

namespace LoggingService.Api;

[ApiController]
public class LogsController : ControllerBase
{
    private readonly ILogRepository _repository;
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogRepository repository, ILogger<LogsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("/api/logs")]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 100, [FromQuery] string? transactionId = null)
    {
        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            return Ok(await _repository.GetByTransactionIdAsync(transactionId));
        }

        return Ok(await _repository.GetRecentAsync(limit));
    }

    [HttpGet("/api/logs/transaction/{transactionId}")]
    public async Task<IActionResult> GetByTransactionId(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return BadRequest(new { error = "Transaction ID is required." });
        }

        return Ok(await _repository.GetByTransactionIdAsync(transactionId));
    }

    [HttpPost("/api/logs")]
    public async Task<IActionResult> Create([FromBody] LogEvent logEvent)
    {
        if (string.IsNullOrWhiteSpace(logEvent.ServiceName) || string.IsNullOrWhiteSpace(logEvent.Message))
        {
            return BadRequest(new { error = "Service name and message are required." });
        }

        var entry = new LogEntry
        {
            EventName = string.IsNullOrWhiteSpace(logEvent.EventName) ? "GenericEvent" : logEvent.EventName,
            ServiceName = logEvent.ServiceName,
            Level = string.IsNullOrWhiteSpace(logEvent.Level) ? "Information" : logEvent.Level,
            Message = logEvent.Message,
            Exception = logEvent.Exception,
            CorrelationId = logEvent.CorrelationId,
            Context = logEvent.Context ?? new Dictionary<string, object?>(),
            CreatedUtc = logEvent.OccurredUtc == default ? DateTime.UtcNow : logEvent.OccurredUtc
        };

        await _repository.AddAsync(entry);
        _logger.LogInformation("Persisted log event {EventName} for service {ServiceName}.", entry.EventName, entry.ServiceName);
        return Ok(new { success = true, id = entry.Id, deduplicated = false });
    }
}
