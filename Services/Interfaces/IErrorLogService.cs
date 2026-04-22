using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;

namespace Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

public interface IErrorLogService
{
    Task WriteAsync(AppErrorEntry entry, CancellationToken cancellationToken = default);
}