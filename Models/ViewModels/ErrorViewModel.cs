namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class ErrorViewModel
{
    public string RequestId { get; set; } = string.Empty;

    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);

    public string? ErrorCode { get; set; }

    public string? Message { get; set; }
}