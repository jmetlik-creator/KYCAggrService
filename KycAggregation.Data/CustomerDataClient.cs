using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KycAggregation.Data;

public sealed class CustomerDataClient : ICustomerDataClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerDataClient> _logger;

    public CustomerDataClient(HttpClient httpClient, ILogger<CustomerDataClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AggregatedKycData> GetAsync(string ssn, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = ssn.Trim();

        if (!IsValidSsn(normalizedSsn))
        {
            throw new InvalidSsnException("The provided SSN is invalid.");
        }

        var personalDetails = await GetPersonalDetailsAsync(normalizedSsn, cancellationToken).ConfigureAwait(false);
        var contactDetails = await GetContactDetailsAsync(normalizedSsn, cancellationToken).ConfigureAwait(false);
        var kycForm = await GetKycFormAsync(normalizedSsn, cancellationToken).ConfigureAwait(false);

        var address = BuildAddress(personalDetails, contactDetails);
        var phoneNumber = SelectPreferredPhoneNumber(contactDetails.PhoneNumbers);
        var email = SelectPreferredEmail(contactDetails.Emails);
        var taxCountry = ExtractTaxCountry(kycForm.Items);
        var income = ExtractIncome(kycForm.Items);

        return new AggregatedKycData
        {
            Ssn = normalizedSsn,
            FirstName = personalDetails.FirstName ?? string.Empty,
            LastName = personalDetails.SurName ?? string.Empty,
            Address = address,
            PhoneNumber = phoneNumber,
            Email = email,
            TaxCountry = taxCountry,
            Income = income,
        };
    }

    private async Task<PersonalDetailsResponse> GetPersonalDetailsAsync(string ssn, CancellationToken cancellationToken)
    {
        var response = await SendAsync($"/personal-details/{ssn}", cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<PersonalDetailsResponse>(response, JsonOptions)
            ?? throw new CustomerNotFoundException("Customer data not found for the provided SSN.");
    }

    private async Task<ContactDetailsResponse> GetContactDetailsAsync(string ssn, CancellationToken cancellationToken)
    {
        var response = await SendAsync($"/contact-details/{ssn}", cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<ContactDetailsResponse>(response, JsonOptions)
            ?? throw new CustomerNotFoundException("Customer data not found for the provided SSN.");
    }

    private async Task<KycFormResponse> GetKycFormAsync(string ssn, CancellationToken cancellationToken)
    {
        var candidateDates = new[]
        {
            DateTime.UtcNow.Date,
            DateTime.UtcNow.AddYears(-1).Date,
            new DateTime(2024, 1, 1),
            new DateTime(2023, 1, 1),
            new DateTime(2022, 1, 1),
        }.Distinct();

        foreach (var date in candidateDates)
        {
            try
            {
                var path = $"/kyc-form/{ssn}/{date:yyyy-MM-dd}";
                var response = await SendAsync(path, cancellationToken).ConfigureAwait(false);
                var payload = JsonSerializer.Deserialize<KycFormResponse>(response, JsonOptions)
                    ?? throw new CustomerNotFoundException("Customer data not found for the provided SSN.");

                if (payload.Items.Count > 0)
                {
                    return payload;
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug(ex, "KYC form not found for SSN {Ssn} on {AsOfDate}.", ssn, date.ToString("yyyy-MM-dd"));
            }
        }

        throw new CustomerNotFoundException("Customer data not found for the provided SSN.");
    }

    private async Task<string> SendAsync(string relativePath, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(relativePath, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new HttpRequestException("The requested customer record was not found.", null, HttpStatusCode.NotFound);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Customer data API returned status code {(int)response.StatusCode}.", null, response.StatusCode);
        }

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsValidSsn(string ssn)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(ssn, "^\\d{8}-\\d{4}$");
    }

    private static string BuildAddress(PersonalDetailsResponse personalDetails, ContactDetailsResponse contactDetails)
    {
        if (!string.IsNullOrWhiteSpace(personalDetails.Address))
        {
            return personalDetails.Address.Trim();
        }

        var firstAddress = contactDetails.Addresses.FirstOrDefault();
        if (firstAddress is null)
        {
            return string.Empty;
        }

        var segments = new[]
        {
            firstAddress.Street,
            firstAddress.PostalCode is null ? null : $"{firstAddress.PostalCode}",
            firstAddress.City,
            firstAddress.State,
            firstAddress.Country
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim());

        return string.Join(", ", segments);
    }

    private static string? SelectPreferredPhoneNumber(List<PhoneNumberResponse> phoneNumbers)
    {
        return phoneNumbers
            .OrderByDescending(x => x.Preferred)
            .Select(x => x.Number)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string? SelectPreferredEmail(List<EmailResponse> emails)
    {
        return emails
            .OrderByDescending(x => x.Preferred)
            .Select(x => x.EmailAddress)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string ExtractTaxCountry(List<KycItemResponse> items)
    {
        var item = items.FirstOrDefault(x =>
            string.Equals(x.Key, "tax_country", StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Key, "taxCountry", StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Key, "tax_country_code", StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(item?.Value) ? string.Empty : item.Value.Trim();
    }

    private static int? ExtractIncome(List<KycItemResponse> items)
    {
        var item = items.FirstOrDefault(x =>
            string.Equals(x.Key, "annual_income", StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Key, "income", StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Key, "annualIncome", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(item?.Value))
        {
            return null;
        }

        return int.TryParse(item.Value.Trim(), out var parsed) ? parsed : null;
    }
}
