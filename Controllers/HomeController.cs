using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;
using Netwise_Rekrutacja_D_Jamrozy.Models.ViewModels;
using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

namespace Netwise_Rekrutacja_D_Jamrozy.Controllers;

public sealed class HomeController : Controller
{
    private readonly ICatFactService catFactService;
    private readonly IFileStorageService fileStorageService;
    private readonly IErrorLogService errorLogService;

    public HomeController(
        ICatFactService catFactService,
        IFileStorageService fileStorageService,
        IErrorLogService errorLogService)
    {
        this.catFactService = catFactService;
        this.fileStorageService = fileStorageService;
        this.errorLogService = errorLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
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

            var model = new HomeViewModel
            {
                Fact = result.Entry?.Fact ?? entry.Fact,
                RetrievedAtUtc = result.Entry?.CreatedAtUtc ?? entry.CreatedAtUtc,
                HasError = false
            };

            return View(model);
        }
        catch (Exception exception)
        {
            await errorLogService.WriteAsync(
                new AppErrorEntry
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    ErrorCode = "HOME-INDEX-500",
                    Message = "Failed to fetch and save cat fact.",
                    Details = exception.ToString(),
                    StatusCode = 500,
                    Path = HttpContext.Request.Path
                },
                cancellationToken);

            var model = new HomeViewModel
            {
                HasError = true,
                ErrorCode = "HOME-INDEX-500",
                ErrorMessage = "The application could not load a new cat fact at this time."
            };

            Response.StatusCode = StatusCodes.Status500InternalServerError;

            return View(model);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorCode = "APP-500",
            Message = "An unexpected error occurred."
        };

        return View(model);
    }
}