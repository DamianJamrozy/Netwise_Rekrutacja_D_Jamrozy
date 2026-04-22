using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;
using Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;

namespace Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

public interface IFileStorageService
{
    Task EnsureStorageReadyAsync(CancellationToken cancellationToken = default);

    string GetFactsFilePath();

    string GetResultsFilePath();

    string GetCrudLogFilePath();

    string GetErrorLogFilePath();

    string GetFactsFileName();

    string GetCrudLogFileName();

    string GetErrorLogFileName();

    IReadOnlyCollection<string> GetAvailableFactFiles();

    IReadOnlyCollection<string> GetAvailableLogFiles();

    Task<FileMutationResult> AppendFactEntryAsync(CatFactEntry entry, CancellationToken cancellationToken = default);

    Task AppendRawApiResponseAsync(CatFactApiResponse response, CancellationToken cancellationToken = default);

    Task<FileMutationResult> CreateManualEntryAsync(string fact, CancellationToken cancellationToken = default);

    Task<FileMutationResult> UpdateFactEntryAsync(Guid id, string fact, CancellationToken cancellationToken = default);

    Task<FileMutationResult> DeleteFactEntryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatFactEntry>> ReadFactEntriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatFactEntry>> ReadFactEntriesAsync(string fileName, CancellationToken cancellationToken = default);

    Task<PagedLogResult> ReadLogLinesPagedAsync(
        string fileName,
        int page,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task PruneExpiredLogsAsync(CancellationToken cancellationToken = default);
}