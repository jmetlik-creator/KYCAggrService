namespace KycAggregation.Data;

public interface IKycDataRepository
{
    Task<CustomerPersonalDetailsRecord?> GetPersonalDetailsAsync(string ssn, CancellationToken cancellationToken = default);
    Task SavePersonalDetailsAsync(CustomerPersonalDetailsRecord record, CancellationToken cancellationToken = default);

    Task<CustomerContactDetailsRecord?> GetContactDetailsAsync(string ssn, CancellationToken cancellationToken = default);
    Task SaveContactDetailsAsync(CustomerContactDetailsRecord record, CancellationToken cancellationToken = default);

    Task<CustomerKycFormRecord?> GetKycFormAsync(string ssn, CancellationToken cancellationToken = default);
    Task SaveKycFormAsync(CustomerKycFormRecord record, CancellationToken cancellationToken = default);

    Task<KycAggregationResultRecord?> GetLatestAggregationAsync(string ssn, CancellationToken cancellationToken = default);
    Task SaveAggregationAsync(KycAggregationResultRecord record, CancellationToken cancellationToken = default);
}
