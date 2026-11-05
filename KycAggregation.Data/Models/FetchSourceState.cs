namespace KycAggregation.Data;

public enum FetchSourceState
{
    FetchedFromDb,
    PartiallyFetchedFromSource,
    DirectFromService
}
