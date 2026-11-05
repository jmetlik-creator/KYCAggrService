namespace KycAggregation.Data;

public interface ICustomerDataClient
{
    Task<AggregatedKycData> GetAsync(string ssn, CancellationToken cancellationToken = default);
    Task<PersonalDetailsResponse> GetPersonalDetailsAsync(string ssn, CancellationToken cancellationToken = default);
    Task<ContactDetailsResponse> GetContactDetailsAsync(string ssn, CancellationToken cancellationToken = default);
    Task<KycFormResponse> GetKycFormAsync(string ssn, CancellationToken cancellationToken = default);
}
