using Microsoft.AspNetCore.Mvc;
using Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Exceptions;
using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;
using Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;
using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

namespace Netwise_Rekrutacja_D_Jamrozy.Controllers;

[Route("api")]
public sealed class ApiController : Controller
{
    private readonly ICatFactService catFactService;
    private readonly IFileStorageService fileStorageService;
    private readonly ICrudAuditService crudAuditService;
    private readonly IErrorLogService errorLogService;
    private readonly IClientIpResolver clientIpResolver;

    public ApiController(
        ICatFactService catFactService,
        IFileStorageService fileStorageService,
        ICrudAuditService crudAuditService,
        IErrorLogService errorLogService,
        IClientIpResolver clientIpResolver)
    {
        this.catFactService = catFactService;
        this.fileStorageService = fileStorageService;
        this.crudAuditService = crudAuditService;
        this.errorLogService = errorLogService;
        this.clientIpResolver = clientIpResolver;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries([FromQuery] string? fileName, CancellationToken cancellationToken)
    {
        try
        {
            var selectedFileName = string.IsNullOrWhiteSpace(fileName) ? fileStorageService.GetFactsFileName() : fileName.Trim();

            var entries = await fileStorageService.ReadFactEntriesAsync(selectedFileName, cancellationToken);

            var response = new EntriesResponse
            {
                Success = true,
                SelectedFileName = selectedFileName,
                Entries = entries
                    .Select(MapToRowViewModel)
                    .ToArray()
            };

            return Ok(response);
        }
        catch (Exception exception)
        {
            await LogErrorAsync(
                "API-ENTRIES-500",
                "Failed to load entries from file storage.",
                exception,
                cancellationToken);

            return StatusCode(StatusCodes.Status500InternalServerError, new EntriesResponse
            {
                Success = false,
                SelectedFileName = fileName ?? fileStorageService.GetFactsFileName(),
                Entries = [],
                ErrorCode = "API-ENTRIES-500",
                Message = "Failed to load entries."
            });
        }
    }

    [HttpPost("facts/fetch")]
    public async Task<IActionResult> FetchFact(CancellationToken cancellationToken)
    {
        try
        {
            var entry = await catFactService.GetNewFactAsync(cancellationToken);
            var result = await fileStorageService.AppendFactEntryAsync(entry, cancellationToken);

            await fileStorageService.AppendRawApiResponseAsync(
                new CatFactApiResponse
                {
                    Fact = entry.Fact,
                    Length = entry.Length
                },
                cancellationToken);

            await WriteCrudAuditAsync(
                operation: "FETCH_ADD",
                lineNumber: result.LineNumber,
                entryId: result.Entry?.Id,
                details: $"Fetched from external API and appended to line {result.LineNumber}. fact=\"{BuildFactPreview(result.Entry?.Fact)}\"",
                cancellationToken: cancellationToken);

            return Ok(new EntryMutationResponse
            {
                Success = true,
                Message = "A new cat fact has been fetched and stored successfully.",
                Entry = result.Entry is null ? null : MapToRowViewModel(result.Entry),
                EntryId = result.Entry?.Id,
                LineNumber = result.LineNumber
            });
        }
        catch (Exception exception)
        {
            await LogErrorAsync(
                "API-FETCH-500",
                "Failed to fetch and store a new cat fact.",
                exception,
                cancellationToken);

            return StatusCode(StatusCodes.Status500InternalServerError, new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-FETCH-500",
                Message = "Failed to fetch a new cat fact."
            });
        }
    }

    [HttpPost("entries/create")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-CREATE-400",
                Message = GetValidationMessage()
            });
        }

        try
        {
            var result = await fileStorageService.CreateManualEntryAsync(request.Fact, cancellationToken);

            await WriteCrudAuditAsync(
                operation: "CREATE",
                lineNumber: result.LineNumber,
                entryId: result.Entry?.Id,
                details: $"Created manual entry at line {result.LineNumber}. fact=\"{BuildFactPreview(result.Entry?.Fact)}\"",
                cancellationToken: cancellationToken);

            return Ok(new EntryMutationResponse
            {
                Success = true,
                Message = "A new entry has been created successfully.",
                Entry = result.Entry is null ? null : MapToRowViewModel(result.Entry),
                EntryId = result.Entry?.Id,
                LineNumber = result.LineNumber
            });
        }
        catch (Exception exception)
        {
            await LogErrorAsync(
                "API-CREATE-500",
                "Failed to create a manual entry.",
                exception,
                cancellationToken);

            return StatusCode(StatusCodes.Status500InternalServerError, new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-CREATE-500",
                Message = "Failed to create a new entry."
            });
        }
    }

    [HttpPost("entries/update")]
    public async Task<IActionResult> UpdateEntry([FromBody] UpdateEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-UPDATE-400",
                Message = GetValidationMessage()
            });
        }

        try
        {
            var result = await fileStorageService.UpdateFactEntryAsync(request.Id, request.Fact, cancellationToken);

            await WriteCrudAuditAsync(
                operation: "UPDATE",
                lineNumber: result.LineNumber,
                entryId: result.Entry?.Id,
                details:
                    $"Updated line {result.LineNumber}. " +
                    $"old=\"{BuildFactPreview(result.PreviousEntry?.Fact)}\" " +
                    $"new=\"{BuildFactPreview(result.Entry?.Fact)}\"",
                cancellationToken: cancellationToken);

            return Ok(new EntryMutationResponse
            {
                Success = true,
                Message = "The entry has been updated successfully.",
                Entry = result.Entry is null ? null : MapToRowViewModel(result.Entry),
                EntryId = result.Entry?.Id,
                LineNumber = result.LineNumber
            });
        }
        catch (KeyNotFoundException exception)
        {
            await LogErrorAsync(
                "API-UPDATE-404",
                "Attempted to update a missing entry.",
                exception,
                cancellationToken,
                StatusCodes.Status404NotFound);

            return NotFound(new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-UPDATE-404",
                Message = "The selected entry was not found."
            });
        }
        catch (Exception exception)
        {
            await LogErrorAsync(
                "API-UPDATE-500",
                "Failed to update an entry.",
                exception,
                cancellationToken);

            return StatusCode(StatusCodes.Status500InternalServerError, new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-UPDATE-500",
                Message = "Failed to update the selected entry."
            });
        }
    }

    [HttpPost("entries/delete")]
    public async Task<IActionResult> DeleteEntry([FromBody] DeleteEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-DELETE-400",
                Message = GetValidationMessage()
            });
        }

        try
        {
            var result = await fileStorageService.DeleteFactEntryAsync(request.Id, cancellationToken);

            await WriteCrudAuditAsync(
                operation: "DELETE",
                lineNumber: result.LineNumber,
                entryId: result.Entry?.Id,
                details: $"Deleted line {result.LineNumber}. removed=\"{BuildFactPreview(result.Entry?.Fact)}\"",
                cancellationToken: cancellationToken);

            return Ok(new EntryMutationResponse
            {
                Success = true,
                Message = "The entry has been deleted successfully.",
                Entry = result.Entry is null ? null : MapToRowViewModel(result.Entry),
                EntryId = result.Entry?.Id,
                LineNumber = result.LineNumber
            });
        }
        catch (KeyNotFoundException exception)
        {
            await LogErrorAsync(
                "API-DELETE-404",
                "Attempted to delete a missing entry.",
                exception,
                cancellationToken,
                StatusCodes.Status404NotFound);

            return NotFound(new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-DELETE-404",
                Message = "The selected entry was not found."
            });
        }
        catch (Exception exception)
        {
            await LogErrorAsync(
                "API-DELETE-500",
                "Failed to delete an entry.",
                exception,
                cancellationToken);

            return StatusCode(StatusCodes.Status500InternalServerError, new EntryMutationResponse
            {
                Success = false,
                ErrorCode = "API-DELETE-500",
                Message = "Failed to delete the selected entry."
            });
        }
    }

    private async Task WriteCrudAuditAsync(
        string operation,
        int lineNumber,
        Guid? entryId,
        string details,
        CancellationToken cancellationToken)
    {
        await crudAuditService.WriteAsync(
            new CrudAuditEntry
            {
                CreatedAtUtc = DateTime.UtcNow,
                ClientIp = clientIpResolver.GetClientIp(),
                Operation = operation,
                TargetFileName = fileStorageService.GetFactsFileName(),
                LineNumber = lineNumber,
                EntryId = entryId,
                Details = details
            },
            cancellationToken);
    }

    private async Task LogErrorAsync(
        string errorCode,
        string message,
        Exception exception,
        CancellationToken cancellationToken,
        int statusCode = StatusCodes.Status500InternalServerError)
    {
        await errorLogService.WriteAsync(
            new AppErrorEntry
            {
                CreatedAtUtc = DateTime.UtcNow,
                ErrorCode = errorCode,
                Message = message,
                Details = exception.ToString(),
                StatusCode = statusCode,
                Path = HttpContext.Request.Path
            },
            cancellationToken);
    }

    private string GetValidationMessage()
    {
        var firstError = ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstError) ? "The request payload is invalid." : firstError;
    }

    private static CatFactRowViewModel MapToRowViewModel(CatFactEntry entry)
    {
        return new CatFactRowViewModel
        {
            Id = entry.Id,
            LineNumber = entry.LineNumber,
            Fact = entry.Fact,
            Length = entry.Length,
            CreatedAtUtc = entry.CreatedAtUtc,
            Source = entry.Source
        };
    }

    private static string BuildFactPreview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= 120 ? normalized : normalized[..120] + "...";
    }
}