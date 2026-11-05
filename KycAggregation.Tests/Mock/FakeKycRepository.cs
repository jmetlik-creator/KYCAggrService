using KycAggregation.Data;

namespace KycAggregation.Tests.Mock;

public sealed class FakeKycRepository : IKycDataRepository
{
    private readonly CustomerPersonalDetailsRecord? _personalDetails;
    private readonly CustomerContactDetailsRecord? _contactDetails;
    private readonly CustomerKycFormRecord? _kycForm;

    public FakeKycRepository(
        CustomerPersonalDetailsRecord? personalDetails = null,
        CustomerContactDetailsRecord? contactDetails = null,
        CustomerKycFormRecord? kycForm = null)
    {
        _personalDetails = personalDetails;
        _contactDetails = contactDetails;
        _kycForm = kycForm;
    }

    public KycAggregationResultRecord? AggregationResult { get; private set; }

    public Task<CustomerPersonalDetailsRecord?> GetPersonalDetailsAsync(string ssn, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_personalDetails is not null && _personalDetails.Ssn == ssn ? _personalDetails : null);
    }

    public Task SavePersonalDetailsAsync(CustomerPersonalDetailsRecord record, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<CustomerContactDetailsRecord?> GetContactDetailsAsync(string ssn, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_contactDetails is not null && _contactDetails.Ssn == ssn ? _contactDetails : null);
    }

    public Task SaveContactDetailsAsync(CustomerContactDetailsRecord record, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<CustomerKycFormRecord?> GetKycFormAsync(string ssn, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_kycForm is not null && _kycForm.Ssn == ssn ? _kycForm : null);
    }

    public Task SaveKycFormAsync(CustomerKycFormRecord record, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<KycAggregationResultRecord?> GetLatestAggregationAsync(string ssn, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AggregationResult is not null && AggregationResult.Ssn == ssn ? AggregationResult : null);
    }

    public Task SaveAggregationAsync(KycAggregationResultRecord record, CancellationToken cancellationToken = default)
    {
        AggregationResult = record;
        return Task.CompletedTask;
    }
}