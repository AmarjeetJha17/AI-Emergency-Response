using System.Text.Json;
using EmergencyResponse.Application.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Services;

public sealed class IncidentAnnotationService : IIncidentAnnotationService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IncidentAnnotationService(
        ApplicationDbContext db,
        ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<IncidentAnnotationResult> AddNoteAsync(
        Guid incidentId,
        string content,
        CancellationToken cancellationToken = default) =>
        AddAsync(
            incidentId,
            content,
            (value, userId) => new IncidentNote
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                Content = value,
                CreatedByUserId = userId
            },
            "IncidentNoteCreated",
            cancellationToken);

    public Task<IncidentAnnotationResult> AddTagAsync(
        Guid incidentId,
        string tag,
        CancellationToken cancellationToken = default) =>
        AddAsync(
            incidentId,
            tag,
            (value, userId) => new IncidentTag
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                Tag = value,
                AddedByUserId = userId
            },
            "IncidentTagAdded",
            cancellationToken);

    private async Task<IncidentAnnotationResult> AddAsync<T>(
        Guid incidentId,
        string value,
        Func<string, string, T> factory,
        string auditAction,
        CancellationToken cancellationToken)
        where T : BaseEntity
    {
        var normalized = value.Trim();
        if (normalized.Length == 0 || normalized.Length > 500)
        {
            throw new ArgumentException(
                "The annotation must contain between 1 and 500 characters.",
                nameof(value));
        }

        if (!await _db.Incidents.AnyAsync(
                x => x.Id == incidentId,
                cancellationToken))
        {
            throw new KeyNotFoundException("The incident was not found.");
        }

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var entity = factory(normalized, userId);
        _db.Set<T>().Add(entity);
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = auditAction,
            EntityName = typeof(T).Name,
            EntityId = entity.Id.ToString(),
            NewValues = JsonSerializer.Serialize(new { incidentId, value = normalized }),
            OccurredAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new IncidentAnnotationResult(
            incidentId,
            normalized,
            entity.CreatedAtUtc);
    }
}
