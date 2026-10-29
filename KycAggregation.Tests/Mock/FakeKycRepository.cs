namespace KycAggregation.Tests.Mock
{
    private sealed class FakeKycRepository : IKycDataRepository
    {
        private readonly PersistedCustomerKycData? _existing;

        public FakeKycRepository(PersistedCustomerKycData? existing = null)
        {
            _existing = existing;
        }

        public bool SaveCalled { get; private set; }

        public Task<PersistedCustomerKycData?> GetBySsnAsync(string ssn, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_existing is not null && _existing.Ssn == ssn ? _existing : null);
        }

        public Task SaveAsync(PersistedCustomerKycData record, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
    }
}