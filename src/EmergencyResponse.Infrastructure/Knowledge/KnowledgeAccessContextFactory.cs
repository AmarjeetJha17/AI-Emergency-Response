using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeAccessContextFactory
    : IKnowledgeAccessContextFactory
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public KnowledgeAccessContextFactory(
        ApplicationDbContext db,
        ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<KnowledgeAccessContext> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required.");
        var user = await _db.Users.SingleOrDefaultAsync(
            x => x.Id == userId,
            cancellationToken);
        var roles = new[]
        {
            "Responder",
            "Operator",
            "EmergencyAdmin",
            "SystemAdmin"
        }.Where(_currentUser.IsInRole).ToList();
        return new KnowledgeAccessContext
        {
            UserId = userId,
            Roles = roles,
            DepartmentId = user?.DepartmentId
        };
    }
}
