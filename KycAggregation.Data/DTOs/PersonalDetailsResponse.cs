using System.Text.Json.Serialization;

namespace KycAggregation.Data;

public sealed class PersonalDetailsResponse
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("surName")]
    public string? SurName { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}
