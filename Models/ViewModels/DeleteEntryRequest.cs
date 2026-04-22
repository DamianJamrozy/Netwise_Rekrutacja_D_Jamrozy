using System.ComponentModel.DataAnnotations;

namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class DeleteEntryRequest
{
    [Required]
    public Guid Id { get; set; }
}