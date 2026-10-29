using KycAggregation.Data;
using Microsoft.Extensions.Logging;
using KycAggregation.Tests.Mock;

namespace KycAggregation.Tests;

public class KycAggregationServiceTests
{
    [Fact]
    public async Task GetAsync_WhenCustomerIsNotPersisted_FetchesAndPersistsRecord()
    {
        var repository = new FakeKycRepository();
        var remoteClient = new FakeCustomerDataClient(new AggregatedKycData
        {
            Ssn = "19800115-1234",
            FirstName = "Erik",
            LastName = "Johansson",
            Address = "Storgatan 1, 111 22 Stockholm",
            PhoneNumber = "+46 70 123 45 67",
            Email = "erik.johansson@example.se",
            TaxCountry = "SE",
            Income = 550000
        });

        var sut = new KycAggregationService(repository, remoteClient, LoggerFactory.Create(_ => { }).CreateLogger<KycAggregationService>());

        var result = await sut.GetAsync("19800115-1234");

        Assert.Equal("Erik", result.FirstName);
        Assert.Equal("Johansson", result.LastName);
        Assert.Equal("SE", result.TaxCountry);
        Assert.Equal(550000, result.Income);
        Assert.True(repository.SaveCalled);
    }

    [Fact]
    public async Task GetAsync_WhenCustomerIsPersisted_UsesCachedData()
    {
        var repository = new FakeKycRepository(new PersistedCustomerKycData
        {
            Ssn = "19900220-5678",
            FirstName = "Anna",
            LastName = "Lindberg",
            Address = "Testgatan 9, 223 10 Malmö",
            PhoneNumber = "+46 70 222 33 44",
            Email = "anna@example.se",
            TaxCountry = "SE",
            Income = 420000,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        });

        var remoteClient = new FakeCustomerDataClient(new AggregatedKycData
        {
            Ssn = "19900220-5678",
            FirstName = "ShouldNot",
            LastName = "BeUsed",
            Address = "Ignored",
            TaxCountry = "DK",
            Income = 999999
        });

        var sut = new KycAggregationService(repository, remoteClient, LoggerFactory.Create(_ => { }).CreateLogger<KycAggregationService>());

        var result = await sut.GetAsync("19900220-5678");

        Assert.Equal("Anna", result.FirstName);
        Assert.Equal("Lindberg", result.LastName);
        Assert.Equal(420000, result.Income);
        Assert.False(remoteClient.WasCalled);
    }
}
