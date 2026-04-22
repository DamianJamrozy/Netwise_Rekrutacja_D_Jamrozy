namespace Netwise_Rekrutacja_D_Jamrozy.Models.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string DataDirectory { get; set; } = "Data";

    public string FactsFileName { get; set; } = "catfacts.txt";

    public string ResultsFileName { get; set; } = "results.txt";

    public string CrudLogFileName { get; set; } = "CRUD.log.txt";

    public string ErrorLogFileName { get; set; } = "log.txt";

    public string[] AllowedFileExtensions { get; set; } = [".txt"];

    public int MaxFactLength { get; set; } = 1000;

    public int MaxManualEntryLength { get; set; } = 1000;
}