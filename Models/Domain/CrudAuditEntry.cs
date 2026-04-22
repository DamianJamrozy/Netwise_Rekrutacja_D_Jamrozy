namespace Netwise_Rekrutacja_D_Jamrozy.Models.Domain;

public sealed class CrudAuditEntry
{
    public DateTime CreatedAtUtc { get; set; }

    public string ClientIp { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string TargetFileName { get; set; } = string.Empty;

    public int LineNumber { get; set; }

    public Guid? EntryId { get; set; }

    public string Details { get; set; } = string.Empty;
}