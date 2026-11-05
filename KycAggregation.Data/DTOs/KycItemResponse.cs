using System.Text.Json.Serialization;

namespace KycAggregation.Data;

public sealed class KycItemResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
