using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentConsolidationService
    : IIncidentConsolidationService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public IncidentConsolidationService(
        ApplicationDbContext db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task ConsolidateDuplicateAsync(
        Guid canonicalIncidentId,
        Guid duplicateIncidentId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (canonicalIncidentId == duplicateIncidentId)
        {
            throw new InvalidOperationException(
                "An incident cannot be consolidated with itself.");
        }

        var canonical =
            await _db.Incidents
                .SingleOrDefaultAsync(
                    x => x.Id == canonicalIncidentId,
                    cancellationToken);

        var duplicate =
            await _db.Incidents
                .SingleOrDefaultAsync(
                    x => x.Id == duplicateIncidentId,
                    cancellationToken);

        if (canonical is null || duplicate is null)
        {
            throw new KeyNotFoundException(
                "One or both incidents could not be found.");
        }

        if (duplicate.CanonicalIncidentId.HasValue)
        {
            throw new InvalidOperationException(
                "The incident has already been consolidated.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        duplicate.IsCanonical = false;
        duplicate.CanonicalIncidentId = canonical.Id;

        _db.IncidentRelations.Add(
            new IncidentRelation
            {
                Id = Guid.NewGuid(),
                ParentIncidentId = canonical.Id,
                RelatedIncidentId = duplicate.Id,
                RelationType = IncidentRelationType.Duplicate,
                Notes = notes,
                CreatedByUserId = _currentUserService.UserId
            });

        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
