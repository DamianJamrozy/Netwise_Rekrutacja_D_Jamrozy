namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class HomeViewModel
{
    public string Fact { get; set; } = string.Empty;

    public DateTime? RetrievedAtUtc { get; set; }

    public bool HasError { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}