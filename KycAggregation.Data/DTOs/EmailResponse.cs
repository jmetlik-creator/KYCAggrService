using System.Text.Json.Serialization;

namespace KycAggregation.Data;

public sealed class EmailResponse
{
    [JsonPropertyName("preferred")]
    public bool Preferred { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
}
