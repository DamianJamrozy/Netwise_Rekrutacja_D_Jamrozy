namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class EntriesResponse
{
    public bool Success { get; set; }

    public string SelectedFileName { get; set; } = string.Empty;

    public IReadOnlyCollection<CatFactRowViewModel> Entries { get; set; } = [];

    public string? ErrorCode { get; set; }

    public string? Message { get; set; }
}