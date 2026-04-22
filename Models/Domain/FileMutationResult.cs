namespace Netwise_Rekrutacja_D_Jamrozy.Models.Domain;

public sealed class FileMutationResult
{
    public CatFactEntry? Entry { get; set; }

    public CatFactEntry? PreviousEntry { get; set; }

    public int LineNumber { get; set; }
}