namespace Netwise_Rekrutacja_D_Jamrozy.Models.Domain;

public sealed class AppErrorEntry
{
    public DateTime CreatedAtUtc { get; set; }

    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }

    public int? StatusCode { get; set; }

    public string? Path { get; set; }
}