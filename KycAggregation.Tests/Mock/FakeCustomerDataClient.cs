using KycAggregation.Data;

namespace KycAggregation.Tests.Mock;

public sealed class FakeCustomerDataClient : ICustomerDataClient
{
    private readonly AggregatedKycData _aggregate;
    private readonly PersonalDetailsResponse _personalDetails;
    private readonly ContactDetailsResponse _contactDetails;
    private readonly KycFormResponse _kycForm;

    public FakeCustomerDataClient(
        AggregatedKycData? aggregate = null,
        PersonalDetailsResponse? personalDetails = null,
        ContactDetailsResponse? contactDetails = null,
        KycFormResponse? kycForm = null)
    {
        _aggregate = aggregate ?? new AggregatedKycData { Ssn = "19800115-1234", FirstName = "Erik", LastName = "Johansson", Address = "Testgatan 1", TaxCountry = "SE", Income = 500000 };
        _personalDetails = personalDetails ?? new PersonalDetailsResponse { FirstName = "Erik", SurName = "Johansson", Address = "Testgatan 1" };
        _contactDetails = contactDetails ?? new ContactDetailsResponse
        {
            Addresses = new List<AddressResponse> { new() { Street = "Testgatan 1", City = "Stockholm", Country = "SE" } },
            Emails = new List<EmailResponse> { new() { Preferred = true, EmailAddress = "erik@example.com" } },
            PhoneNumbers = new List<PhoneNumberResponse> { new() { Preferred = true, Number = "+46 70 123 45 67" } }
        };
        _kycForm = kycForm ?? new KycFormResponse
        {
            Items = new List<KycItemResponse>
            {
                new() { Key = "tax_country", Value = "SE" },
                new() { Key = "annual_income", Value = "500000" }
            }
        };
    }

    public Task<AggregatedKycData> GetAsync(string ssn, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_aggregate with { Ssn = ssn });
    }

    public Task<PersonalDetailsResponse> GetPersonalDetailsAsync(string ssn, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_personalDetails);
    }

    public Task<ContactDetailsResponse> GetContactDetailsAsync(string ssn, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_contactDetails);
    }

    public Task<KycFormResponse> GetKycFormAsync(string ssn, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_kycForm);
    }
}
