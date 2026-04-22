namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class CatFactRowViewModel
{
    public Guid Id { get; set; }

    public int LineNumber { get; set; }

    public string Fact { get; set; } = string.Empty;

    public int Length { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string Source { get; set; } = string.Empty;
}