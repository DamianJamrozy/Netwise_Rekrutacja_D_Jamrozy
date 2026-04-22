using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;

namespace Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

public interface ICrudAuditService
{
    Task WriteAsync(CrudAuditEntry entry, CancellationToken cancellationToken = default);
}