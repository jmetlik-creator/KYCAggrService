using System.ComponentModel.DataAnnotations;

namespace KycAggregation.Data;

public sealed class KycAggregationResultRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransactionId { get; set; }

    [Required]
    [StringLength(32)]
    public string Ssn { get; set; } = string.Empty;

    [Required]
    public string AggregatedPayloadJson { get; set; } = string.Empty;

    [Required]
    public string FetchState { get; set; } = FetchSourceState.FetchedFromDb.ToString();

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
