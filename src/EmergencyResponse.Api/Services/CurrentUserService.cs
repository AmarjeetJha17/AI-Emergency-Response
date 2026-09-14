using System.Security.Claims;

using EmergencyResponse.Application.Interfaces;

namespace EmergencyResponse.Api.Services;

public sealed class CurrentUserService
    : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?
            .User
            .Identity?
            .IsAuthenticated
        ?? false;

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?
            .User
            .IsInRole(role)
        ?? false;
}