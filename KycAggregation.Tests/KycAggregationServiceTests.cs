using KycAggregation.Data;
using KycAggregation.Tests.Mock;
using Logging.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KycAggregation.Tests;

public sealed class KycAggregationServiceLogicTests
{
    [Fact]
    public async Task GetAsync_WhenSourcesAreFresh_UsesPersistedData()
    {
        var repository = CreateRepository();
        var remoteClient = new FakeCustomerDataClient();
        var sut = CreateService(repository, remoteClient, cacheOptions: new KycDataCacheOptions
        {
            PersonalDetailsValidityHours = 2,
            ContactDetailsValidityHours = 2,
            KycFormValidityHours = 2
        });

        var result = await sut.GetAsync("19800115-1234");

        Assert.Equal("Erik", result.FirstName);
        Assert.Equal("Johansson", result.LastName);
        Assert.Equal("SE", result.TaxCountry);
        Assert.Equal(550000, result.Income);
    }

    [Fact]
    public async Task GetAsync_WhenSourceDataIsStale_RefreshesFromRemoteApi()
    {
        var repository = CreateRepository(
            personalFetchedUtc: DateTime.UtcNow.AddHours(-5),
            contactFetchedUtc: DateTime.UtcNow.AddHours(-5),
            kycFetchedUtc: DateTime.UtcNow.AddHours(-5),
            personalJson: "{\"firstName\":\"Old\",\"surName\":\"Value\",\"address\":\"Old Street\"}",
            contactJson: "{\"addresses\":[{\"street\":\"Old Street\",\"city\":\"Gothenburg\",\"country\":\"SE\"}],\"emails\":[{\"preferred\":true,\"emailAddress\":\"old@example.com\"}],\"phoneNumbers\":[{\"preferred\":true,\"number\":\"+46 70 000 00 00\"}]}",
            kycJson: "{\"items\":[{\"key\":\"tax_country\",\"value\":\"SE\"},{\"key\":\"annual_income\",\"value\":\"100\"}]}");

        var remoteClient = new FakeCustomerDataClient(
            aggregate: new AggregatedKycData { Ssn = "19800115-1234", FirstName = "Fresh", LastName = "Value", Address = "New address", TaxCountry = "SE", Income = 500000 },
            personalDetails: new PersonalDetailsResponse { FirstName = "Fresh", SurName = "Value", Address = "New address" },
            contactDetails: new ContactDetailsResponse
            {
                Addresses = new List<AddressResponse> { new() { Street = "New address", City = "Stockholm", Country = "SE" } },
                Emails = new List<EmailResponse> { new() { Preferred = true, EmailAddress = "fresh@example.com" } },
                PhoneNumbers = new List<PhoneNumberResponse> { new() { Preferred = true, Number = "+46 70 111 11 11" } }
            },
            kycForm: new KycFormResponse
            {
                Items = new List<KycItemResponse>
                {
                    new() { Key = "tax_country", Value = "SE" },
                    new() { Key = "annual_income", Value = "500000" }
                }
            });

        var sut = CreateService(repository, remoteClient, cacheOptions: new KycDataCacheOptions
        {
            PersonalDetailsValidityHours = 1,
            ContactDetailsValidityHours = 1,
            KycFormValidityHours = 1
        });

        var result = await sut.GetAsync("19800115-1234");

        Assert.Equal("Fresh", result.FirstName);
        Assert.Equal("New address", result.Address);
    }

    [Fact]
    public async Task GetAsync_WhenSomeSourcesAreFreshAndSomeAreStale_UsesPartiallyFetchedFromSourceState()
    {
        var repository = CreateRepository(
            personalFetchedUtc: DateTime.UtcNow.AddHours(-1),
            contactFetchedUtc: DateTime.UtcNow.AddHours(-5),
            kycFetchedUtc: DateTime.UtcNow.AddHours(-5),
            personalJson: "{\"firstName\":\"Erik\",\"surName\":\"Johansson\",\"address\":\"Storgatan 1\"}",
            contactJson: "{\"addresses\":[{\"street\":\"Old St\",\"city\":\"Gothenburg\",\"country\":\"SE\"}],\"emails\":[{\"preferred\":true,\"emailAddress\":\"old@example.com\"}],\"phoneNumbers\":[{\"preferred\":true,\"number\":\"+46 70 000 00 00\"}]}",
            kycJson: "{\"items\":[{\"key\":\"tax_country\",\"value\":\"SE\"},{\"key\":\"annual_income\",\"value\":\"100\"}]}"
        );

        var remoteClient = new FakeCustomerDataClient(
            aggregate: new AggregatedKycData { Ssn = "19800115-1234", FirstName = "Erik", LastName = "Johansson", Address = "New St", TaxCountry = "SE", Income = 500000 },
            personalDetails: new PersonalDetailsResponse { FirstName = "Erik", SurName = "Johansson", Address = "Storgatan 1" },
            contactDetails: new ContactDetailsResponse
            {
                Addresses = new List<AddressResponse> { new() { Street = "New St", City = "Stockholm", Country = "SE" } },
                Emails = new List<EmailResponse> { new() { Preferred = true, EmailAddress = "fresh@example.com" } },
                PhoneNumbers = new List<PhoneNumberResponse> { new() { Preferred = true, Number = "+46 70 111 11 11" } }
            },
            kycForm: new KycFormResponse
            {
                Items = new List<KycItemResponse>
                {
                    new() { Key = "tax_country", Value = "SE" },
                    new() { Key = "annual_income", Value = "500000" }
                }
            });

        var sut = CreateService(repository, remoteClient, cacheOptions: new KycDataCacheOptions
        {
            PersonalDetailsValidityHours = 2,
            ContactDetailsValidityHours = 1,
            KycFormValidityHours = 1
        });

        var result = await sut.GetAsync("19800115-1234");

        Assert.Equal(FetchSourceState.PartiallyFetchedFromSource.ToString(), result.FetchState);
        Assert.Equal("New St", result.Address);
    }

    [Fact]
    public async Task GetFullAsync_WhenDataExists_ReturnsLatestPersonalContactAndKycData()
    {
        var repository = CreateRepository();
        var sut = CreateService(repository, new FakeCustomerDataClient());

        var result = await sut.GetFullAsync("19800115-1234");

        Assert.Equal("19800115-1234", result.Ssn);
        Assert.Equal("Erik", result.PersonalDetails.FirstName);
        Assert.Equal("Johansson", result.PersonalDetails.SurName);
        Assert.Equal("Storgatan 1", result.ContactDetails.Addresses.Single().Street);
        Assert.Equal("SE", result.KycForm.Items.Single(x => x.Key == "tax_country").Value);
    }

    [Fact]
    public async Task GetAsync_WhenSsnIsBlank_ThrowsInvalidSsnException()
    {
        var sut = CreateService(new FakeKycRepository(), new FakeCustomerDataClient());

        await Assert.ThrowsAsync<InvalidSsnException>(() => sut.GetAsync("   "));
    }

    [Fact]
    public async Task GetFullAsync_WhenSsnIsInvalidFormat_ThrowsInvalidSsnException()
    {
        var sut = CreateService(new FakeKycRepository(), new FakeCustomerDataClient());

        await Assert.ThrowsAsync<InvalidSsnException>(() => sut.GetFullAsync("invalid-ssn"));
    }

    private static FakeKycRepository CreateRepository(
        string ssn = "19800115-1234",
        DateTime? personalFetchedUtc = null,
        DateTime? contactFetchedUtc = null,
        DateTime? kycFetchedUtc = null,
        string personalJson = "{\"firstName\":\"Erik\",\"surName\":\"Johansson\",\"address\":\"Storgatan 1\"}",
        string contactJson = "{\"addresses\":[{\"street\":\"Storgatan 1\",\"city\":\"Stockholm\",\"country\":\"SE\"}],\"emails\":[{\"preferred\":true,\"emailAddress\":\"erik@example.com\"}],\"phoneNumbers\":[{\"preferred\":true,\"number\":\"+46 70 123 45 67\"}]}",
        string kycJson = "{\"items\":[{\"key\":\"tax_country\",\"value\":\"SE\"},{\"key\":\"annual_income\",\"value\":\"550000\"}]}")
    {
        return new FakeKycRepository(
            new CustomerPersonalDetailsRecord
            {
                Ssn = ssn,
                PayloadJson = personalJson,
                FetchedUtc = personalFetchedUtc ?? DateTime.UtcNow.AddHours(-1)
            },
            new CustomerContactDetailsRecord
            {
                Ssn = ssn,
                PayloadJson = contactJson,
                FetchedUtc = contactFetchedUtc ?? DateTime.UtcNow.AddHours(-1)
            },
            new CustomerKycFormRecord
            {
                Ssn = ssn,
                PayloadJson = kycJson,
                FetchedUtc = kycFetchedUtc ?? DateTime.UtcNow.AddHours(-1)
            });
    }

    private static KycAggregationService CreateService(
        FakeKycRepository repository,
        ICustomerDataClient remoteClient,
        ILogSink? logSink = null,
        KycDataCacheOptions? cacheOptions = null)
    {
        return new KycAggregationService(
            repository,
            remoteClient,
            LoggerFactory.Create(_ => { }).CreateLogger<KycAggregationService>(),
            logSink,
            Options.Create(cacheOptions ?? new KycDataCacheOptions()));
    }
}

