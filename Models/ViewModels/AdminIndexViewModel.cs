namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class AdminIndexViewModel
{
    public string SelectedFileName { get; set; } = string.Empty;

    public string ActiveTab { get; set; } = "entries";

    public IReadOnlyCollection<string> AvailableFiles { get; set; } = [];

    public IReadOnlyCollection<CatFactRowViewModel> Entries { get; set; } = [];

    public string FactsFileName { get; set; } = string.Empty;

    public IReadOnlyCollection<string> AvailableLogFiles { get; set; } = [];

    public string SelectedLogFileName { get; set; } = string.Empty;

    public AdminLogFileViewModel? SelectedLogFile { get; set; }
}