using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Logging.Contracts;

public sealed class LogEvent
{
    public string EventName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object?> Context { get; set; } = new();
}

public sealed class LogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? CorrelationId { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public string ContextJson { get; set; } = "{}";

    [NotMapped]
    [JsonIgnore]
    public Dictionary<string, object?> Context
    {
        get => string.IsNullOrWhiteSpace(ContextJson)
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(ContextJson) ?? new Dictionary<string, object?>();
        set => ContextJson = JsonSerializer.Serialize(value ?? new Dictionary<string, object?>());
    }
}

public interface ILogSink
{
    Task WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default);
}
