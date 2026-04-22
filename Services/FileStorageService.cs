using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Exceptions;
using Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Security;
using Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Serialization;
using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;
using Netwise_Rekrutacja_D_Jamrozy.Models.Options;
using Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;
using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

namespace Netwise_Rekrutacja_D_Jamrozy.Services;

public sealed class FileStorageService : IFileStorageService
{
    private static readonly SemaphoreSlim fileSemaphore = new(1, 1);
    private const int LogRetentionDays = 30;

    private readonly IWebHostEnvironment environment;
    private readonly StorageOptions storageOptions;

    public FileStorageService(IWebHostEnvironment environment, IOptions<StorageOptions> storageOptions)
    {
        this.environment = environment;
        this.storageOptions = storageOptions.Value;
    }

    public async Task EnsureStorageReadyAsync(CancellationToken cancellationToken = default)
    {
        var dataDirectoryPath = GetDataDirectoryPath();

        if (!Directory.Exists(dataDirectoryPath))
        {
            Directory.CreateDirectory(dataDirectoryPath);
        }

        await EnsureFileExistsAsync(GetFactsFilePath(), cancellationToken);
        await EnsureFileExistsAsync(GetResultsFilePath(), cancellationToken);
        await EnsureFileExistsAsync(GetCrudLogFilePath(), cancellationToken);
        await EnsureFileExistsAsync(GetErrorLogFilePath(), cancellationToken);
    }

    public string GetFactsFilePath()
    {
        return BuildSafePath(storageOptions.FactsFileName);
    }

    public string GetResultsFilePath()
    {
        return BuildSafePath(storageOptions.ResultsFileName);
    }

    public string GetCrudLogFilePath()
    {
        return BuildSafePath(storageOptions.CrudLogFileName);
    }

    public string GetErrorLogFilePath()
    {
        return BuildSafePath(storageOptions.ErrorLogFileName);
    }

    public string GetFactsFileName()
    {
        return storageOptions.FactsFileName;
    }

    public string GetCrudLogFileName()
    {
        return storageOptions.CrudLogFileName;
    }

    public string GetErrorLogFileName()
    {
        return storageOptions.ErrorLogFileName;
    }

    public IReadOnlyCollection<string> GetAvailableFactFiles()
    {
        var dataDirectoryPath = GetDataDirectoryPath();

        if (!Directory.Exists(dataDirectoryPath))
        {
            return [];
        }

        var allowedExtensions = new HashSet<string>(
            storageOptions.AllowedFileExtensions.Select(value => value.Trim().ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        var excludedFiles = new HashSet<string>(
        [
            storageOptions.CrudLogFileName,
            storageOptions.ErrorLogFileName,
            storageOptions.ResultsFileName
        ],
        StringComparer.OrdinalIgnoreCase);

        var fileNames = Directory
            .EnumerateFiles(dataDirectoryPath, "*", SearchOption.TopDirectoryOnly)
            .Where(filePath => allowedExtensions.Contains(Path.GetExtension(filePath)))
            .Select(Path.GetFileName)
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Cast<string>()
            .Where(fileName => !excludedFiles.Contains(fileName))
            .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return fileNames;
    }

    public IReadOnlyCollection<string> GetAvailableLogFiles()
    {
        return
        [
            storageOptions.ErrorLogFileName,
            storageOptions.CrudLogFileName
        ];
    }

    public async Task<FileMutationResult> AppendFactEntryAsync(CatFactEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await EnsureStorageReadyAsync(cancellationToken);

        var normalizedEntry = NormalizeEntry(entry);
        var filePath = GetFactsFilePath();

        await fileSemaphore.WaitAsync(cancellationToken);

        try
        {
            var existingEntries = await ReadEntriesNoLockAsync(filePath, cancellationToken);
            normalizedEntry.LineNumber = existingEntries.Count + 1;

            var serializedEntry = JsonLineSerializer.Serialize(normalizedEntry);
            await File.AppendAllTextAsync(filePath, serializedEntry + Environment.NewLine, Encoding.UTF8, cancellationToken);

            return new FileMutationResult
            {
                Entry = CloneEntry(normalizedEntry),
                LineNumber = normalizedEntry.LineNumber
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new StorageOperationException("Failed to append fact entry to storage.", exception);
        }
        finally
        {
            fileSemaphore.Release();
        }
    }

    public async Task AppendRawApiResponseAsync(CatFactApiResponse response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        await EnsureStorageReadyAsync(cancellationToken);

        var normalizedFact = NormalizeFact(response.Fact);

        var normalizedResponse = new CatFactApiResponse
        {
            Fact = normalizedFact,
            Length = response.Length > 0 ? response.Length : normalizedFact.Length
        };

        var serialized = JsonLineSerializer.Serialize(normalizedResponse);
        var filePath = GetResultsFilePath();

        await fileSemaphore.WaitAsync(cancellationToken);

        try
        {
            await File.AppendAllTextAsync(
                filePath,
                serialized + Environment.NewLine,
                Encoding.UTF8,
                cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new StorageOperationException("Failed to append raw API response to results storage.", exception);
        }
        finally
        {
            fileSemaphore.Release();
        }
    }

    public async Task<FileMutationResult> CreateManualEntryAsync(string fact, CancellationToken cancellationToken = default)
    {
        var entry = new CatFactEntry
        {
            Id = Guid.NewGuid(),
            Fact = fact,
            Length = fact?.Trim().Length ?? 0,
            CreatedAtUtc = DateTime.UtcNow,
            Source = "manual"
        };

        return await AppendFactEntryAsync(entry, cancellationToken);
    }

    public async Task<FileMutationResult> UpdateFactEntryAsync(Guid id, string fact, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new StorageOperationException("Entry id cannot be empty.");
        }

        await EnsureStorageReadyAsync(cancellationToken);

        var normalizedFact = NormalizeFact(fact);
        var filePath = GetFactsFilePath();

        await fileSemaphore.WaitAsync(cancellationToken);

        try
        {
            var entries = await ReadEntriesNoLockAsync(filePath, cancellationToken);
            var index = entries.FindIndex(entry => entry.Id == id);

            if (index < 0)
            {
                throw new KeyNotFoundException($"Entry with id '{id}' was not found.");
            }

            var previousEntry = CloneEntry(entries[index]);

            entries[index].Fact = normalizedFact;
            entries[index].Length = normalizedFact.Length;
            entries[index].LineNumber = index + 1;

            await RewriteEntriesNoLockAsync(filePath, entries, cancellationToken);

            return new FileMutationResult
            {
                Entry = CloneEntry(entries[index]),
                PreviousEntry = previousEntry,
                LineNumber = entries[index].LineNumber
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new StorageOperationException("Failed to update fact entry in storage.", exception);
        }
        finally
        {
            fileSemaphore.Release();
        }
    }

    public async Task<FileMutationResult> DeleteFactEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new StorageOperationException("Entry id cannot be empty.");
        }

        await EnsureStorageReadyAsync(cancellationToken);

        var filePath = GetFactsFilePath();

        await fileSemaphore.WaitAsync(cancellationToken);

        try
        {
            var entries = await ReadEntriesNoLockAsync(filePath, cancellationToken);
            var index = entries.FindIndex(entry => entry.Id == id);

            if (index < 0)
            {
                throw new KeyNotFoundException($"Entry with id '{id}' was not found.");
            }

            var removedEntry = CloneEntry(entries[index]);
            var removedLineNumber = entries[index].LineNumber;

            entries.RemoveAt(index);

            for (var i = 0; i < entries.Count; i++)
            {
                entries[i].LineNumber = i + 1;
            }

            await RewriteEntriesNoLockAsync(filePath, entries, cancellationToken);

            return new FileMutationResult
            {
                Entry = removedEntry,
                PreviousEntry = removedEntry,
                LineNumber = removedLineNumber
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new StorageOperationException("Failed to delete fact entry from storage.", exception);
        }
        finally
        {
            fileSemaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<CatFactEntry>> ReadFactEntriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);

        var filePath = GetFactsFilePath();

        await fileSemaphore.WaitAsync(cancellationToken);

        try
        {
            var entries = await ReadEntriesNoLockAsync(filePath, cancellationToken);

            return entries
                .OrderByDescending(entry => entry.CreatedAtUtc)
                .ToArray();
        }
        finally
        {
            fileSemaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<CatFactEntry>> ReadFactEntriesAsync(string fileName, CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);

        var filePath = BuildSafePath(fileName);

        await fileSemaphore.WaitAsync(cancellationToken);

        try
        {
            var entries = await ReadEntriesNoLockAsync(filePath, cancellationToken);

            return entries
                .OrderByDescending(entry => entry.CreatedAtUtc)
                .ToArray();
        }
        finally
        {
            fileSemaphore.Release();
        }
    }

    public async Task<PagedLogResult> ReadLogLinesPagedAsync(
        string fileName,
        int page,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);
        await PruneExpiredLogsAsync(cancellationToken);

        if (page <= 0)
        {
            page = 1;
        }

        if (pageSize <= 0 || pageSize > 100)
        {
            pageSize = 20;
        }

        var allowedLogFiles = new HashSet<string>(
            [storageOptions.CrudLogFileName, storageOptions.ErrorLogFileName],
            StringComparer.OrdinalIgnoreCase);

        if (!allowedLogFiles.Contains(fileName))
        {
            throw new StorageOperationException($"Log file is not allowed: {fileName}");
        }

        var filePath = BuildSafePath(fileName);

        await fileSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(filePath))
            {
                return new PagedLogResult
                {
                    Lines = [],
                    CurrentPage = 1,
                    PageSize = pageSize,
                    TotalLines = 0,
                    TotalPages = 0
                };
            }

            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

            var filteredLines = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Reverse()
                .ToArray();

            var totalLines = filteredLines.Length;
            var totalPages = totalLines == 0 ? 0 : (int)Math.Ceiling(totalLines / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var pagedLines = filteredLines
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return new PagedLogResult
            {
                Lines = pagedLines,
                CurrentPage = totalPages == 0 ? 1 : page,
                PageSize = pageSize,
                TotalLines = totalLines,
                TotalPages = totalPages
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new StorageOperationException("Failed to read log lines from storage.", exception);
        }
        finally
        {
            fileSemaphore.Release();
        }
    }

    public async Task PruneExpiredLogsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);

        var cutoffDateUtc = DateTime.UtcNow.AddDays(-LogRetentionDays);

        foreach (var fileName in GetAvailableLogFiles())
        {
            var filePath = BuildSafePath(fileName);

            await fileSemaphore.WaitAsync(cancellationToken);

            try
            {
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

                var keptLines = lines
                    .Where(line => ShouldKeepLogLine(line, cutoffDateUtc))
                    .ToArray();

                if (keptLines.Length == lines.Length)
                {
                    continue;
                }

                await File.WriteAllLinesAsync(filePath, keptLines, new UTF8Encoding(false), cancellationToken);
            }
            finally
            {
                fileSemaphore.Release();
            }
        }
    }

    private static bool ShouldKeepLogLine(string line, DateTime cutoffDateUtc)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var timestampPrefix = "timestampUtc=";
        var parts = line.Split(" | ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var timestampPart = parts.FirstOrDefault(part => part.StartsWith(timestampPrefix, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(timestampPart))
        {
            return true;
        }

        var timestampValue = timestampPart[timestampPrefix.Length..];

        if (!DateTime.TryParse(
                timestampValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var parsedTimestampUtc))
        {
            return true;
        }

        return parsedTimestampUtc >= cutoffDateUtc;
    }

    private async Task<List<CatFactEntry>> ReadEntriesNoLockAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        var entries = new List<CatFactEntry>();

        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: true);

        using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

        var physicalLineNumber = 0;

        while (!streamReader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await streamReader.ReadLineAsync(cancellationToken);
            physicalLineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var entry = JsonLineSerializer.Deserialize<CatFactEntry>(line);

            if (entry is null)
            {
                continue;
            }

            if (entry.Id == Guid.Empty || string.IsNullOrWhiteSpace(entry.Fact))
            {
                continue;
            }

            entry.Fact = NormalizeFact(entry.Fact);
            entry.Length = entry.Length > 0 ? entry.Length : entry.Fact.Length;
            entry.CreatedAtUtc = entry.CreatedAtUtc == default ? DateTime.UtcNow : entry.CreatedAtUtc;
            entry.Source = string.IsNullOrWhiteSpace(entry.Source) ? "unknown" : entry.Source.Trim();
            entry.LineNumber = physicalLineNumber;

            entries.Add(entry);
        }

        return entries;
    }

    private async Task RewriteEntriesNoLockAsync(
        string filePath,
        IReadOnlyCollection<CatFactEntry> entries,
        CancellationToken cancellationToken)
    {
        var tempFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var fileStream = new FileStream(
                tempFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            await using (var streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false)))
            {
                foreach (var entry in entries.OrderBy(entry => entry.LineNumber))
                {
                    var normalizedEntry = NormalizeEntry(entry);
                    var serializedEntry = JsonLineSerializer.Serialize(normalizedEntry);
                    await streamWriter.WriteLineAsync(serializedEntry.AsMemory(), cancellationToken);
                }

                await streamWriter.FlushAsync(cancellationToken);
            }

            File.Move(tempFilePath, filePath, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new StorageOperationException("Failed to rewrite storage file.", exception);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private CatFactEntry NormalizeEntry(CatFactEntry entry)
    {
        var fact = NormalizeFact(entry.Fact);

        return new CatFactEntry
        {
            Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            Fact = fact,
            Length = fact.Length,
            CreatedAtUtc = entry.CreatedAtUtc == default ? DateTime.UtcNow : entry.CreatedAtUtc,
            Source = string.IsNullOrWhiteSpace(entry.Source) ? "unknown" : entry.Source.Trim(),
            LineNumber = entry.LineNumber
        };
    }

    private string NormalizeFact(string? fact)
    {
        var normalizedFact = fact?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedFact))
        {
            throw new StorageOperationException("Fact cannot be empty.");
        }

        if (normalizedFact.Length > storageOptions.MaxManualEntryLength)
        {
            throw new StorageOperationException($"Fact exceeds maximum allowed length of {storageOptions.MaxManualEntryLength} characters.");
        }

        return normalizedFact;
    }

    private static CatFactEntry CloneEntry(CatFactEntry entry)
    {
        return new CatFactEntry
        {
            Id = entry.Id,
            Fact = entry.Fact,
            Length = entry.Length,
            CreatedAtUtc = entry.CreatedAtUtc,
            Source = entry.Source,
            LineNumber = entry.LineNumber
        };
    }

    private string GetDataDirectoryPath()
    {
        var contentRootPath = environment.ContentRootPath;
        var configuredDirectory = storageOptions.DataDirectory?.Trim();

        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            throw new StorageOperationException("Storage data directory is not configured.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(contentRootPath, configuredDirectory));

        if (!fullPath.StartsWith(contentRootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new StorageOperationException("Resolved data directory is outside of content root.");
        }

        return fullPath;
    }

    private string BuildSafePath(string fileName)
    {
        if (!RequestValidationHelpers.IsSafeFileName(fileName))
        {
            throw new StorageOperationException($"Unsafe file name detected: {fileName}");
        }

        var extension = Path.GetExtension(fileName);
        var isAllowedExtension = storageOptions.AllowedFileExtensions.Any(value => string.Equals(value, extension, StringComparison.OrdinalIgnoreCase));

        if (!isAllowedExtension)
        {
            throw new StorageOperationException($"File extension is not allowed: {fileName}");
        }

        var dataDirectoryPath = GetDataDirectoryPath();
        var fullPath = Path.GetFullPath(Path.Combine(dataDirectoryPath, fileName));

        if (!fullPath.StartsWith(dataDirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new StorageOperationException("Resolved file path is outside of data directory.");
        }

        return fullPath;
    }

    private static async Task EnsureFileExistsAsync(string filePath, CancellationToken cancellationToken)
    {
        if (File.Exists(filePath))
        {
            return;
        }

        await using var fileStream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        await fileStream.FlushAsync(cancellationToken);
    }
}