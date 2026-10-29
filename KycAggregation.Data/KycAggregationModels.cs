using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KycAggregation.Data;

public record AggregatedKycData
{
    public string Ssn { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string TaxCountry { get; init; } = string.Empty;
    public int? Income { get; init; }
}

public sealed class PersistedCustomerKycData
{
    [Key]
    [StringLength(32)]
    public string Ssn { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [StringLength(200)]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? Email { get; set; }

    [Required]
    [StringLength(10)]
    public string TaxCountry { get; set; } = string.Empty;

    public int? Income { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public interface IKycDataRepository
{
    Task<PersistedCustomerKycData?> GetBySsnAsync(string ssn, CancellationToken cancellationToken = default);
    Task SaveAsync(PersistedCustomerKycData record, CancellationToken cancellationToken = default);
}

public interface ICustomerDataClient
{
    Task<AggregatedKycData> GetAsync(string ssn, CancellationToken cancellationToken = default);
}

public sealed class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(string message) : base(message)
    {
    }
}

public sealed class InvalidSsnException : Exception
{
    public InvalidSsnException(string message) : base(message)
    {
    }
}

internal sealed class PersonalDetailsResponse
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("surName")]
    public string? SurName { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

internal sealed class ContactDetailsResponse
{
    [JsonPropertyName("addresses")]
    public List<AddressResponse> Addresses { get; set; } = new();

    [JsonPropertyName("emails")]
    public List<EmailResponse> Emails { get; set; } = new();

    [JsonPropertyName("phoneNumbers")]
    public List<PhoneNumberResponse> PhoneNumbers { get; set; } = new();
}

internal sealed class AddressResponse
{
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

internal sealed class EmailResponse
{
    [JsonPropertyName("preferred")]
    public bool Preferred { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
}

internal sealed class PhoneNumberResponse
{
    [JsonPropertyName("preferred")]
    public bool Preferred { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }
}

internal sealed class KycFormResponse
{
    [JsonPropertyName("items")]
    public List<KycItemResponse> Items { get; set; } = new();
}

internal sealed class KycItemResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
