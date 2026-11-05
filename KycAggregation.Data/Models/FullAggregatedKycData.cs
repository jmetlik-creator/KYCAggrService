namespace KycAggregation.Data;

public sealed record FullAggregatedKycData
{
    public string Ssn { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string TaxCountry { get; set; } = string.Empty;
    public int? Income { get; set; }
    public string FetchState { get; set; } = string.Empty;
    public PersonalDetailsResponse PersonalDetails { get; set; } = new();
    public ContactDetailsResponse ContactDetails { get; set; } = new();
    public KycFormResponse KycForm { get; set; } = new();
}
