using System.ComponentModel.DataAnnotations;

namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class CreateEntryRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Fact { get; set; } = string.Empty;
}