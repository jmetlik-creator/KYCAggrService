using System.Text.Json.Serialization;

namespace KycAggregation.Data;

public sealed class PhoneNumberResponse
{
    [JsonPropertyName("preferred")]
    public bool Preferred { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }
}
