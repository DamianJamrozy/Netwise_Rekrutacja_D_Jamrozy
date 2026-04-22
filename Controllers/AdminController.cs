using Microsoft.AspNetCore.Mvc;
using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;
using Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;
using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

namespace Netwise_Rekrutacja_D_Jamrozy.Controllers;

public sealed class AdminController : Controller
{
    private readonly IFileStorageService fileStorageService;
    private readonly IErrorLogService errorLogService;

    public AdminController(
        IFileStorageService fileStorageService,
        IErrorLogService errorLogService)
    {
        this.fileStorageService = fileStorageService;
        this.errorLogService = errorLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? fileName,
        string? tab,
        string? logFileName,
        int logPage = 1,
        int logPageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeTab = ResolveActiveTab(tab);
            var selectedFileName = ResolveSelectedFileName(fileName);
            var selectedLogFileName = ResolveSelectedLogFileName(logFileName);

            var entries = await fileStorageService.ReadFactEntriesAsync(selectedFileName, cancellationToken);
            var selectedLogFile = await BuildSelectedLogFileAsync(selectedLogFileName, logPage, logPageSize, cancellationToken);

            var model = new AdminIndexViewModel
            {
                ActiveTab = activeTab,
                SelectedFileName = selectedFileName,
                FactsFileName = fileStorageService.GetFactsFileName(),
                AvailableFiles = fileStorageService.GetAvailableFactFiles(),
                Entries = entries
                    .Select(MapToRowViewModel)
                    .ToArray(),
                AvailableLogFiles = fileStorageService.GetAvailableLogFiles(),
                SelectedLogFileName = selectedLogFileName,
                SelectedLogFile = selectedLogFile
            };

            return View(model);
        }
        catch (Exception exception)
        {
            await errorLogService.WriteAsync(
                new AppErrorEntry
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    ErrorCode = "ADMIN-INDEX-500",
                    Message = "Failed to load admin page data.",
                    Details = exception.ToString(),
                    StatusCode = 500,
                    Path = HttpContext.Request.Path
                },
                cancellationToken);

            Response.StatusCode = StatusCodes.Status500InternalServerError;

            var model = new AdminIndexViewModel
            {
                ActiveTab = "entries",
                SelectedFileName = fileStorageService.GetFactsFileName(),
                FactsFileName = fileStorageService.GetFactsFileName(),
                AvailableFiles = fileStorageService.GetAvailableFactFiles(),
                Entries = [],
                AvailableLogFiles = fileStorageService.GetAvailableLogFiles(),
                SelectedLogFileName = fileStorageService.GetErrorLogFileName(),
                SelectedLogFile = null
            };

            return View(model);
        }
    }

    private async Task<AdminLogFileViewModel> BuildSelectedLogFileAsync(
        string logFileName,
        int logPage,
        int logPageSize,
        CancellationToken cancellationToken)
    {
        var pagedResult = await fileStorageService.ReadLogLinesPagedAsync(
            logFileName,
            logPage,
            logPageSize,
            cancellationToken);

        return new AdminLogFileViewModel
        {
            FileName = logFileName,
            CurrentPage = pagedResult.CurrentPage,
            PageSize = pagedResult.PageSize,
            TotalLines = pagedResult.TotalLines,
            TotalPages = pagedResult.TotalPages,
            Lines = pagedResult.Lines
                .Select((content, index) => new AdminLogLineViewModel
                {
                    LineNumber = ((pagedResult.CurrentPage - 1) * pagedResult.PageSize) + index + 1,
                    Content = content
                })
                .ToArray()
        };
    }

    private string ResolveSelectedFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return fileStorageService.GetFactsFileName();
        }

        return fileName.Trim();
    }

    private string ResolveSelectedLogFileName(string? logFileName)
    {
        if (string.IsNullOrWhiteSpace(logFileName))
        {
            return fileStorageService.GetErrorLogFileName();
        }

        return logFileName.Trim();
    }

    private static string ResolveActiveTab(string? tab)
    {
        return string.Equals(tab, "logs", StringComparison.OrdinalIgnoreCase) ? "logs" : "entries";
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
}