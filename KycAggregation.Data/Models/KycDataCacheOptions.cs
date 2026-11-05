namespace KycAggregation.Data;

public sealed class KycDataCacheOptions
{
    public int PersonalDetailsValidityHours { get; set; } = 24;
    public int ContactDetailsValidityHours { get; set; } = 24;
    public int KycFormValidityHours { get; set; } = 24;
}
