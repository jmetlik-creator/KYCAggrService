using System.Text.Json.Serialization;

namespace KycAggregation.Data;

public sealed class KycFormResponse
{
    [JsonPropertyName("items")]
    public List<KycItemResponse> Items { get; set; } = new();
}
