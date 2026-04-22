using System.Text;
using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;
using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

namespace Netwise_Rekrutacja_D_Jamrozy.Services;

public sealed class CrudAuditService : ICrudAuditService
{
    private readonly IFileStorageService fileStorageService;
    private static readonly SemaphoreSlim semaphore = new(1, 1);

    public CrudAuditService(IFileStorageService fileStorageService)
    {
        this.fileStorageService = fileStorageService;
    }

    public async Task WriteAsync(CrudAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await fileStorageService.EnsureStorageReadyAsync(cancellationToken);
        await fileStorageService.PruneExpiredLogsAsync(cancellationToken);

        var filePath = fileStorageService.GetCrudLogFilePath();
        var line = BuildLogLine(entry);

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            await File.AppendAllTextAsync(filePath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string BuildLogLine(CrudAuditEntry entry)
    {
        var parts = new List<string>
        {
            $"timestampUtc={entry.CreatedAtUtc:O}",
            $"clientIp={Sanitize(entry.ClientIp)}",
            $"operation={Sanitize(entry.Operation)}",
            $"targetFile={Sanitize(entry.TargetFileName)}",
            $"lineNumber={entry.LineNumber}"
        };

        if (entry.EntryId.HasValue)
        {
            parts.Add($"entryId={entry.EntryId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Details))
        {
            parts.Add($"details={Sanitize(entry.Details)}");
        }

        return string.Join(" | ", parts);
    }

    private static string Sanitize(string value)
    {
        return value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }
}