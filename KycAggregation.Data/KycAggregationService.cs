using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Logging.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KycAggregation.Data;

public sealed class KycAggregationService
{
    private readonly IKycDataRepository _repository;
    private readonly ICustomerDataClient _customerDataClient;
    private readonly ILogger<KycAggregationService> _logger;
    private readonly ILogSink? _logSink;
    private readonly KycDataCacheOptions _cacheOptions;

    public KycAggregationService(
        IKycDataRepository repository,
        ICustomerDataClient customerDataClient,
        ILogger<KycAggregationService> logger,
        ILogSink? logSink = null,
        IOptions<KycDataCacheOptions>? cacheOptions = null)
    {
        _repository = repository;
        _customerDataClient = customerDataClient;
        _logger = logger;
        _logSink = logSink;
        _cacheOptions = cacheOptions?.Value ?? new KycDataCacheOptions();
    }

    public async Task<AggregatedKycData> GetAsync(string ssn, Guid? transactionId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeAndValidateSsn(ssn);
        var activeTransactionId = transactionId ?? Guid.NewGuid();

        var (personalDetails, personalFetchState) = await GetOrRefreshPersonalDetailsAsync(normalizedSsn, activeTransactionId, cancellationToken).ConfigureAwait(false);
        var (contactDetails, contactFetchState) = await GetOrRefreshContactDetailsAsync(normalizedSsn, activeTransactionId, cancellationToken).ConfigureAwait(false);
        var (kycForm, kycFetchState) = await GetOrRefreshKycFormAsync(normalizedSsn, activeTransactionId, cancellationToken).ConfigureAwait(false);

        var fetchState = DetermineFetchState(personalFetchState, contactFetchState, kycFetchState);
        var aggregate = BuildAggregatedRecord(normalizedSsn, personalDetails, contactDetails, kycForm, fetchState);

        await _repository.SaveAggregationAsync(new KycAggregationResultRecord
        {
            TransactionId = activeTransactionId,
            Ssn = normalizedSsn,
            AggregatedPayloadJson = JsonSerializer.Serialize(aggregate),
            FetchState = fetchState.ToString(),
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken).ConfigureAwait(false);

        await EmitLogAsync(
            "KycAggregateCompleted",
            "KycAggregationService",
            "Information",
            "KYC aggregate completed.",
            activeTransactionId,
            new Dictionary<string, object?>
            {
                ["ssn"] = normalizedSsn,
                ["transactionId"] = activeTransactionId,
                ["fetchState"] = fetchState.ToString(),
                ["personalState"] = personalFetchState,
                ["contactState"] = contactFetchState,
                ["kycState"] = kycFetchState
            },
            cancellationToken).ConfigureAwait(false);

        return aggregate;
    }

    public async Task<FullAggregatedKycData> GetFullAsync(string ssn, Guid? transactionId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSsn = NormalizeAndValidateSsn(ssn);
        var activeTransactionId = transactionId ?? Guid.NewGuid();

        var (personalDetails, personalFetchState) = await GetOrRefreshPersonalDetailsAsync(normalizedSsn, activeTransactionId, cancellationToken).ConfigureAwait(false);
        var (contactDetails, contactFetchState) = await GetOrRefreshContactDetailsAsync(normalizedSsn, activeTransactionId, cancellationToken).ConfigureAwait(false);
        var (kycForm, kycFetchState) = await GetOrRefreshKycFormAsync(normalizedSsn, activeTransactionId, cancellationToken).ConfigureAwait(false);

        var fetchState = DetermineFetchState(personalFetchState, contactFetchState, kycFetchState);
        var fullAggregate = BuildFullAggregatedRecord(normalizedSsn, personalDetails, contactDetails, kycForm, fetchState);

        await _repository.SaveAggregationAsync(new KycAggregationResultRecord
        {
            TransactionId = activeTransactionId,
            Ssn = normalizedSsn,
            AggregatedPayloadJson = JsonSerializer.Serialize(fullAggregate),
            FetchState = fetchState.ToString(),
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken).ConfigureAwait(false);

        return fullAggregate;
    }

    private async Task<(CustomerPersonalDetailsRecord Record, FetchSourceState SourceState)> GetOrRefreshPersonalDetailsAsync(string ssn, Guid transactionId, CancellationToken cancellationToken)
    {
        var record = await _repository.GetPersonalDetailsAsync(ssn, cancellationToken).ConfigureAwait(false);
        if (record is not null && !IsStale(record.FetchedUtc, _cacheOptions.PersonalDetailsValidityHours))
        {
            _logger.LogInformation("Using personal details from DB for SSN {Ssn}.", ssn);
            return (record, FetchSourceState.FetchedFromDb);
        }

        var remote = await _customerDataClient.GetPersonalDetailsAsync(ssn, cancellationToken).ConfigureAwait(false);
        var persisted = new CustomerPersonalDetailsRecord
        {
            Ssn = ssn,
            TransactionId = transactionId,
            PayloadJson = JsonSerializer.Serialize(remote),
            FetchedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.SavePersonalDetailsAsync(persisted, cancellationToken).ConfigureAwait(false);
        return (persisted, FetchSourceState.DirectFromService);
    }

    private async Task<(CustomerContactDetailsRecord Record, FetchSourceState SourceState)> GetOrRefreshContactDetailsAsync(string ssn, Guid transactionId, CancellationToken cancellationToken)
    {
        var record = await _repository.GetContactDetailsAsync(ssn, cancellationToken).ConfigureAwait(false);
        if (record is not null && !IsStale(record.FetchedUtc, _cacheOptions.ContactDetailsValidityHours))
        {
            _logger.LogInformation("Using contact details from DB for SSN {Ssn}.", ssn);
            return (record, FetchSourceState.FetchedFromDb);
        }

        var remote = await _customerDataClient.GetContactDetailsAsync(ssn, cancellationToken).ConfigureAwait(false);
        var persisted = new CustomerContactDetailsRecord
        {
            Ssn = ssn,
            TransactionId = transactionId,
            PayloadJson = JsonSerializer.Serialize(remote),
            FetchedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.SaveContactDetailsAsync(persisted, cancellationToken).ConfigureAwait(false);
        return (persisted, FetchSourceState.DirectFromService);
    }

    private async Task<(CustomerKycFormRecord Record, FetchSourceState SourceState)> GetOrRefreshKycFormAsync(string ssn, Guid transactionId, CancellationToken cancellationToken)
    {
        var record = await _repository.GetKycFormAsync(ssn, cancellationToken).ConfigureAwait(false);
        if (record is not null && !IsStale(record.FetchedUtc, _cacheOptions.KycFormValidityHours))
        {
            _logger.LogInformation("Using KYC form data from DB for SSN {Ssn}.", ssn);
            return (record, FetchSourceState.FetchedFromDb);
        }

        var remote = await _customerDataClient.GetKycFormAsync(ssn, cancellationToken).ConfigureAwait(false);
        var persisted = new CustomerKycFormRecord
        {
            Ssn = ssn,
            TransactionId = transactionId,
            PayloadJson = JsonSerializer.Serialize(remote),
            FetchedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.SaveKycFormAsync(persisted, cancellationToken).ConfigureAwait(false);
        return (persisted, FetchSourceState.DirectFromService);
    }

    private static string NormalizeAndValidateSsn(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn))
        {
            throw new InvalidSsnException("An SSN is required.");
        }

        var normalizedSsn = ssn.Trim();
        if (!Regex.IsMatch(normalizedSsn, "^\\d{8}-\\d{4}$"))
        {
            throw new InvalidSsnException("The provided SSN is invalid.");
        }

        return normalizedSsn;
    }

    private static bool IsStale(DateTime fetchedUtc, int validityHours)
    {
        return DateTime.UtcNow - fetchedUtc > TimeSpan.FromHours(validityHours);
    }

    private static FetchSourceState DetermineFetchState(FetchSourceState personalState, FetchSourceState contactState, FetchSourceState kycState)
    {
        var directFromServiceCount = 0;
        var fetchedFromDbCount = 0;

        foreach (var state in new[] { personalState, contactState, kycState })
        {
            if (state == FetchSourceState.DirectFromService)
            {
                directFromServiceCount++;
            }
            else if (state == FetchSourceState.FetchedFromDb)
            {
                fetchedFromDbCount++;
            }
        }

        if (directFromServiceCount > 0 && fetchedFromDbCount > 0)
        {
            return FetchSourceState.PartiallyFetchedFromSource;
        }

        if (directFromServiceCount == 3)
        {
            return FetchSourceState.DirectFromService;
        }

        return FetchSourceState.FetchedFromDb;
    }

    private static AggregatedKycData BuildAggregatedRecord(
        string ssn,
        CustomerPersonalDetailsRecord personalDetails,
        CustomerContactDetailsRecord contactDetails,
        CustomerKycFormRecord kycForm,
        FetchSourceState fetchState)
    {
        var personal = JsonSerializer.Deserialize<PersonalDetailsResponse>(personalDetails.PayloadJson)
            ?? throw new CustomerDataApiException("Customer personal details were not found.", HttpStatusCode.NotFound);

        var contact = JsonSerializer.Deserialize<ContactDetailsResponse>(contactDetails.PayloadJson)
            ?? throw new CustomerDataApiException("Customer contact details were not found.", HttpStatusCode.NotFound);

        var kyc = JsonSerializer.Deserialize<KycFormResponse>(kycForm.PayloadJson)
            ?? throw new CustomerDataApiException("Customer KYC form data was not found.", HttpStatusCode.NotFound);

        var address = BuildAddress(personal, contact);
        var phoneNumber = SelectPreferredPhoneNumber(contact.PhoneNumbers);
        var email = SelectPreferredEmail(contact.Emails);
        var taxCountry = ExtractTaxCountry(kyc.Items);
        var income = ExtractIncome(kyc.Items);

        return new AggregatedKycData
        {
            Ssn = ssn,
            FirstName = personal.FirstName ?? string.Empty,
            LastName = personal.SurName ?? string.Empty,
            Address = address,
            PhoneNumber = phoneNumber,
            Email = email,
            TaxCountry = taxCountry,
            Income = income,
            FetchState = fetchState.ToString(),
        };
    }

    private static FullAggregatedKycData BuildFullAggregatedRecord(
        string ssn,
        CustomerPersonalDetailsRecord personalDetails,
        CustomerContactDetailsRecord contactDetails,
        CustomerKycFormRecord kycForm,
        FetchSourceState fetchState)
    {
        var personal = JsonSerializer.Deserialize<PersonalDetailsResponse>(personalDetails.PayloadJson)
            ?? throw new CustomerDataApiException("Customer personal details were not found.", HttpStatusCode.NotFound);

        var contact = JsonSerializer.Deserialize<ContactDetailsResponse>(contactDetails.PayloadJson)
            ?? throw new CustomerDataApiException("Customer contact details were not found.", HttpStatusCode.NotFound);

        var kyc = JsonSerializer.Deserialize<KycFormResponse>(kycForm.PayloadJson)
            ?? throw new CustomerDataApiException("Customer KYC form data was not found.", HttpStatusCode.NotFound);

        var address = BuildAddress(personal, contact);
        var phoneNumber = SelectPreferredPhoneNumber(contact.PhoneNumbers);
        var email = SelectPreferredEmail(contact.Emails);
        var taxCountry = ExtractTaxCountry(kyc.Items);
        var income = ExtractIncome(kyc.Items);

        return new FullAggregatedKycData
        {
            Ssn = ssn,
            FirstName = personal.FirstName ?? string.Empty,
            LastName = personal.SurName ?? string.Empty,
            Address = address,
            PhoneNumber = phoneNumber,
            Email = email,
            TaxCountry = taxCountry,
            Income = income,
            FetchState = fetchState.ToString(),
            PersonalDetails = personal,
            ContactDetails = contact,
            KycForm = kyc
        };
    }

    private static string BuildAddress(PersonalDetailsResponse personalDetails, ContactDetailsResponse contactDetails)
    {
        var firstAddress = contactDetails.Addresses.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstAddress?.Street))
        {
            return firstAddress.Street.Trim();
        }

        if (!string.IsNullOrWhiteSpace(personalDetails.Address))
        {
            return personalDetails.Address.Trim();
        }

        return string.Empty;
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

    private async Task EmitLogAsync(
        string eventName,
        string serviceName,
        string level,
        string message,
        Guid transactionId,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        if (_logSink is null)
        {
            return;
        }

        await _logSink.WriteAsync(new LogEvent
        {
            EventName = eventName,
            ServiceName = serviceName,
            Level = level,
            Message = message,
            CorrelationId = transactionId.ToString(),
            OccurredUtc = DateTime.UtcNow,
            Context = context
        }, cancellationToken).ConfigureAwait(false);
    }
}
