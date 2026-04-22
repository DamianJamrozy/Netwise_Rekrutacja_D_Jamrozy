using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;

namespace Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

public interface ICatFactService
{
    Task<CatFactEntry> GetNewFactAsync(CancellationToken cancellationToken = default);
}