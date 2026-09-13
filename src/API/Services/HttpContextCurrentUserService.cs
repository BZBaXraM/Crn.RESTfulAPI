using System.Security.Claims;
using Application.Interfaces;

namespace API.Services;

public sealed class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name) ?? User?.Identity?.Name;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
