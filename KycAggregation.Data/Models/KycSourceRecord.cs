using System.ComponentModel.DataAnnotations;

namespace KycAggregation.Data;

public abstract class KycSourceRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(32)]
    public string Ssn { get; set; } = string.Empty;

    public Guid TransactionId { get; set; }

    public DateTime FetchedUtc { get; set; } = DateTime.UtcNow;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
