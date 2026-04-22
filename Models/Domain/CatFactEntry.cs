using System.Text.Json.Serialization;

namespace Netwise_Rekrutacja_D_Jamrozy.Models.Domain;

public sealed class CatFactEntry
{
    public Guid Id { get; set; }

    public string Fact { get; set; } = string.Empty;

    public int Length { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string Source { get; set; } = string.Empty;

    [JsonIgnore]
    public int LineNumber { get; set; }
}