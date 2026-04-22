using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

namespace Netwise_Rekrutacja_D_Jamrozy.Services;

public sealed class ClientIpResolver : IClientIpResolver
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public ClientIpResolver(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public string GetClientIp()
    {
        var context = httpContextAccessor.HttpContext;
        var ipAddress = context?.Connection.RemoteIpAddress;

        return ipAddress?.ToString() ?? "unknown";
    }
}