using System.ComponentModel.DataAnnotations;

namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class UpdateEntryRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Fact { get; set; } = string.Empty;
}