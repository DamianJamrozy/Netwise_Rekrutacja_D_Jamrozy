namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class EntryMutationResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public CatFactRowViewModel? Entry { get; set; }

    public Guid? EntryId { get; set; }

    public int? LineNumber { get; set; }
}