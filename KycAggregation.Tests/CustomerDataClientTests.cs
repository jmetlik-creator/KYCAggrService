using System.Net;
using KycAggregation.Data;
using KycAggregation.Tests.Mock;
using Microsoft.Extensions.Logging;

namespace KycAggregation.Tests;

public sealed class CustomerDataClientTests
{
    [Fact]
    public async Task GetAsync_WhenAllSourceResponsesArePresent_ReturnsAggregatedCustomerData()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["/personal-details/19800115-1234"] = "{\"firstName\":\"Erik\",\"surName\":\"Johansson\",\"address\":\"Storgatan 1\"}",
            ["/contact-details/19800115-1234"] = "{\"addresses\":[{\"street\":\"Storgatan 1\",\"city\":\"Stockholm\",\"country\":\"SE\"}],\"emails\":[{\"preferred\":true,\"emailAddress\":\"erik@example.com\"}],\"phoneNumbers\":[{\"preferred\":true,\"number\":\"+46 70 123 45 67\"}]}",
            ["/kyc-form/19800115-1234"] = "{\"items\":[{\"key\":\"tax_country\",\"value\":\"SE\"},{\"key\":\"annual_income\",\"value\":\"550000\"}]}"
        });

        var result = await sut.GetAsync("19800115-1234");

        Assert.Equal("Erik", result.FirstName);
        Assert.Equal("Johansson", result.LastName);
        Assert.Equal("Storgatan 1", result.Address);
        Assert.Equal("SE", result.TaxCountry);
        Assert.Equal(550000, result.Income);
    }

    [Fact]
    public async Task GetPersonalDetailsAsync_WhenRemoteReturnsNotFound_ThrowsCustomerDataApiExceptionWithHttpStatusCode()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<CustomerDataApiException>(() => sut.GetPersonalDetailsAsync("19800115-1234"));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static CustomerDataClient CreateSut(Dictionary<string, string>? responses = null)
    {
        var handler = new StubHttpMessageHandler(responses ?? new Dictionary<string, string>());
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test")
        };

        return new CustomerDataClient(client, LoggerFactory.Create(_ => { }).CreateLogger<CustomerDataClient>());
    }

}
