using System.Text;
using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;
using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

namespace Netwise_Rekrutacja_D_Jamrozy.Services;

public sealed class ErrorLogService : IErrorLogService
{
    private readonly IFileStorageService fileStorageService;
    private static readonly SemaphoreSlim semaphore = new(1, 1);

    public ErrorLogService(IFileStorageService fileStorageService)
    {
        this.fileStorageService = fileStorageService;
    }

    public async Task WriteAsync(AppErrorEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await fileStorageService.EnsureStorageReadyAsync(cancellationToken);
        await fileStorageService.PruneExpiredLogsAsync(cancellationToken);

        var filePath = fileStorageService.GetErrorLogFilePath();
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

    private static string BuildLogLine(AppErrorEntry entry)
    {
        var parts = new List<string>
        {
            $"timestampUtc={entry.CreatedAtUtc:O}",
            $"errorCode={Sanitize(entry.ErrorCode)}",
            $"message={Sanitize(entry.Message)}"
        };

        if (entry.StatusCode.HasValue)
        {
            parts.Add($"statusCode={entry.StatusCode.Value}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Path))
        {
            parts.Add($"path={Sanitize(entry.Path)}");
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