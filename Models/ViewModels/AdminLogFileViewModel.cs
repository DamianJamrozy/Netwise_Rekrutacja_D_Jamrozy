namespace Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

public sealed class AdminLogFileViewModel
{
    public string FileName { get; set; } = string.Empty;

    public IReadOnlyCollection<AdminLogLineViewModel> Lines { get; set; } = [];

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalLines { get; set; }

    public int TotalPages { get; set; }
}