namespace Netwise_Rekrutacja_D_Jamrozy.Models.Options;

public sealed class CatFactApiOptions
{
    public const string SectionName = "CatFactApi";

    public string BaseUrl { get; set; } = string.Empty;

    public string FactEndpoint { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 15;
}