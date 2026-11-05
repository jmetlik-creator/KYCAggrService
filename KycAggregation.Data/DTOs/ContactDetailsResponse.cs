using System.Text.Json.Serialization;

namespace KycAggregation.Data;

public sealed class ContactDetailsResponse
{
    [JsonPropertyName("addresses")]
    public List<AddressResponse> Addresses { get; set; } = new();

    [JsonPropertyName("emails")]
    public List<EmailResponse> Emails { get; set; } = new();

    [JsonPropertyName("phoneNumbers")]
    public List<PhoneNumberResponse> PhoneNumbers { get; set; } = new();
}
