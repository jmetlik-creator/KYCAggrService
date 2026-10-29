using Microsoft.Extensions.Logging;

namespace KycAggregation.Data;

public sealed class KycAggregationService
{
    private readonly IKycDataRepository _repository;
    private readonly ICustomerDataClient _customerDataClient;
    private readonly ILogger<KycAggregationService> _logger;

    public KycAggregationService(
        IKycDataRepository repository,
        ICustomerDataClient customerDataClient,
        ILogger<KycAggregationService> logger)
    {
        _repository = repository;
        _customerDataClient = customerDataClient;
        _logger = logger;
    }

    public async Task<AggregatedKycData> GetAsync(string ssn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssn))
        {
            throw new InvalidSsnException("An SSN is required.");
        }

        var normalizedSsn = ssn.Trim();
        var cached = await _repository.GetBySsnAsync(normalizedSsn, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            _logger.LogInformation("Returning cached KYC data for SSN {Ssn}.", normalizedSsn);
            return MapToAggregatedKycData(cached);
        }

        _logger.LogInformation("KYC data not found in local store for SSN {Ssn}; fetching from remote API.", normalizedSsn);
        var remoteData = await _customerDataClient.GetAsync(normalizedSsn, cancellationToken).ConfigureAwait(false);

        var persistedRecord = new PersistedCustomerKycData
        {
            Ssn = remoteData.Ssn,
            FirstName = remoteData.FirstName,
            LastName = remoteData.LastName,
            Address = remoteData.Address,
            PhoneNumber = remoteData.PhoneNumber,
            Email = remoteData.Email,
            TaxCountry = remoteData.TaxCountry,
            Income = remoteData.Income,
        };

        await _repository.SaveAsync(persistedRecord, cancellationToken).ConfigureAwait(false);
        return remoteData;
    }

    private static AggregatedKycData MapToAggregatedKycData(PersistedCustomerKycData record)
    {
        return new AggregatedKycData
        {
            Ssn = record.Ssn,
            FirstName = record.FirstName,
            LastName = record.LastName,
            Address = record.Address,
            PhoneNumber = record.PhoneNumber,
            Email = record.Email,
            TaxCountry = record.TaxCountry,
            Income = record.Income,
        };
    }
}
