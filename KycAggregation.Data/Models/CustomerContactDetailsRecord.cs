using System.ComponentModel.DataAnnotations;

namespace KycAggregation.Data;

public sealed class CustomerContactDetailsRecord : KycSourceRecord
{
    [Required]
    public string PayloadJson { get; set; } = string.Empty;
}
